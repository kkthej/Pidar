using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Pidar.Models.Xnat;

namespace Pidar.Services.Xnat;

public sealed class XnatMultiService : IXnatMultiService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<XnatMultiService> _logger;

    public XnatMultiService(
        HttpClient http,
        IMemoryCache cache,
        IConfiguration config,
        ILogger<XnatMultiService> logger)
    {
        _http = http;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    public async Task<IReadOnlyList<XnatPublicProject>> GetAllPublicProjectsAsync(CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync("xnat_all_public_projects_v5", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

            var instances = _config.GetSection("XnatInstances").Get<List<XnatInstanceOptions>>() ?? new();

            var tasks = instances
                .Where(x => !string.IsNullOrWhiteSpace(x.BaseUrl))
                .Select(inst => FetchInstanceProjectsAsync(inst, ct));

            var allResults = await Task.WhenAll(tasks);

            return (IReadOnlyList<XnatPublicProject>)allResults
                .SelectMany(r => r)
                .OrderBy(x => x.InstanceName)
                .ThenBy(x => x.Id)
                .ToList();

        }) ?? Array.Empty<XnatPublicProject>();
    }

    public async Task<bool> IsListedPublicProjectAsync(string instanceKey, string projectId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(instanceKey) || string.IsNullOrWhiteSpace(projectId))
            return false;

        var projects = await GetAllPublicProjectsAsync(ct);
        return projects.Any(p =>
            string.Equals(p.InstanceKey, instanceKey, StringComparison.Ordinal) &&
            string.Equals(p.Id, projectId, StringComparison.Ordinal));
    }

    // ---- Fetch all projects from a single XNAT instance ----
    private async Task<List<XnatPublicProject>> FetchInstanceProjectsAsync(XnatInstanceOptions inst, CancellationToken ct)
    {
        var results = new List<XnatPublicProject>();
        var baseUrl = inst.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/data/projects?format=json";

        string? authToken = null;
        if (!string.IsNullOrWhiteSpace(inst.Username) && !string.IsNullOrWhiteSpace(inst.Password))
        {
            authToken = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{inst.Username}:{inst.Password}"));
        }

        try
        {
            var json = await GetJsonHandling202Async(url, baseUrl, inst.Key, inst.Name, ct, authToken);
            if (string.IsNullOrWhiteSpace(json))
                return results;

            var parsed = JsonSerializer.Deserialize<XnatProjectsApiResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var rows = parsed?.ResultSet?.Result ?? new();

            // With credentials, /data/projects returns every project that account can see
            // (private and protected included), so each one must be confirmed as public.
            // Without credentials the guest listing already contains only public projects.
            HashSet<string>? publicIds = null;
            if (authToken != null)
                publicIds = await GetPublicProjectIdsAsync(inst, baseUrl, rows, authToken, ct);

            foreach (var p in rows)
            {
                if (string.IsNullOrWhiteSpace(p.ID)) continue;
                if (publicIds != null && !publicIds.Contains(p.ID)) continue;

                var uri = p.URI ?? $"/data/projects/{p.ID}";

                var piFullName = $"{p.pi_firstname} {p.pi_lastname}".Trim();
                if (string.IsNullOrWhiteSpace(piFullName))
                    piFullName = null;

                results.Add(new XnatPublicProject(
                    InstanceKey: inst.Key,
                    InstanceName: inst.Name,
                    InstanceBaseUrl: baseUrl,
                    Id: p.ID,
                    Description: p.description,
                    PiFullName: piFullName,
                    Uri: uri
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "XNAT instance {Key} ({Name}) crashed while fetching projects. URL={Url}",
                inst.Key, inst.Name, url);
        }

        return results;
    }

    // ---- Keep only projects whose accessibility is exactly "public" ----
    private async Task<HashSet<string>> GetPublicProjectIdsAsync(
        XnatInstanceOptions inst,
        string baseUrl,
        List<XnatProjectsApiResponse.ProjectRow> rows,
        string authToken,
        CancellationToken ct)
    {
        using var throttle = new SemaphoreSlim(4); // be gentle with partner servers

        var checks = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.ID))
            .Select(async r =>
            {
                await throttle.WaitAsync(ct);
                try
                {
                    var isPublic = await IsProjectPublicAsync(inst, baseUrl, r.ID, authToken, ct);
                    return (r.ID, isPublic);
                }
                finally
                {
                    throttle.Release();
                }
            });

        var results = await Task.WhenAll(checks);

        var hidden = results.Count(x => !x.isPublic);
        if (hidden > 0)
            _logger.LogInformation(
                "XNAT {Key} ({Name}): {Hidden} of {Total} visible projects are not public and were excluded.",
                inst.Key, inst.Name, hidden, results.Length);

        return results.Where(x => x.isPublic).Select(x => x.ID).ToHashSet(StringComparer.Ordinal);
    }

    // Fail closed: any error means "not public". Results are cached for an hour so the
    // 9-minute warmup does not re-check every project on every cycle.
    private async Task<bool> IsProjectPublicAsync(
        XnatInstanceOptions inst,
        string baseUrl,
        string projectId,
        string authToken,
        CancellationToken ct)
    {
        var cacheKey = $"xnat_accessibility_v1::{inst.Key}::{projectId}";
        if (_cache.TryGetValue(cacheKey, out bool cached))
            return cached;

        var url = $"{baseUrl}/data/projects/{Uri.EscapeDataString(projectId)}/accessibility";
        bool isPublic;
        TimeSpan ttl;

        try
        {
            using var resp = await SendXnatGetAsync(url, baseUrl, ct, authToken);
            if (resp.IsSuccessStatusCode)
            {
                var body = (await resp.Content.ReadAsStringAsync(ct)).Trim();
                isPublic = body.Equals("public", StringComparison.OrdinalIgnoreCase);
                ttl = TimeSpan.FromHours(1);
            }
            else
            {
                _logger.LogWarning(
                    "XNAT {Key} ({Name}) accessibility check failed for {Project}. Status={Status}. Treating as not public.",
                    inst.Key, inst.Name, projectId, (int)resp.StatusCode);
                isPublic = false;
                ttl = TimeSpan.FromMinutes(5); // retry errors sooner
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex,
                "XNAT {Key} ({Name}) accessibility check crashed for {Project}. Treating as not public.",
                inst.Key, inst.Name, projectId);
            isPublic = false;
            ttl = TimeSpan.FromMinutes(5);
        }

        _cache.Set(cacheKey, isPublic, ttl);
        return isPublic;
    }

    // ---- Core logic: GET JSON with robust 202 handling ----
    private async Task<string?> GetJsonHandling202Async(
        string url,
        string baseUrl,
        string instKey,
        string instName,
        CancellationToken ct,
        string? authToken = null)
    {
        using var firstResp = await SendXnatGetAsync(url, baseUrl, ct, authToken);

        if ((int)firstResp.StatusCode == 202)
        {
            var location = firstResp.Headers.Location?.ToString();

            if (!string.IsNullOrWhiteSpace(location))
            {
                if (location.StartsWith("/"))
                    location = baseUrl + location;

                _logger.LogInformation(
                    "XNAT {Key} ({Name}) returned 202 with Location. Polling Location={Location}",
                    instKey, instName, location);

                using var finalResp = await PollUrlUntilNot202Async(location, baseUrl, instKey, instName, ct, authToken);
                if (!finalResp.IsSuccessStatusCode)
                {
                    var err = await finalResp.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning(
                        "XNAT {Key} ({Name}) poll(Location) failed. Status={Status}. Location={Location}. Body(first 500)={Body}",
                        instKey, instName, (int)finalResp.StatusCode, location,
                        err.Length > 500 ? err[..500] : err);
                    return null;
                }

                return await finalResp.Content.ReadAsStringAsync(ct);
            }

            _logger.LogWarning(
                "XNAT {Key} ({Name}) returned 202 but no Location header. Will retry same URL with backoff. URL={Url}",
                instKey, instName, url);

            return await RetrySameUrlUntil200Async(url, baseUrl, instKey, instName, ct, authToken);
        }

        if (!firstResp.IsSuccessStatusCode)
        {
            var err = await firstResp.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "XNAT {Key} ({Name}) failed. Status={Status}. URL={Url}. Body(first 500)={Body}",
                instKey, instName, (int)firstResp.StatusCode, url,
                err.Length > 500 ? err[..500] : err);
            return null;
        }

        return await firstResp.Content.ReadAsStringAsync(ct);
    }

    // ---- Retry the same URL if server keeps returning 202 without Location ----
    private async Task<string?> RetrySameUrlUntil200Async(
        string url,
        string baseUrl,
        string instKey,
        string instName,
        CancellationToken ct,
        string? authToken = null)
    {
        var delaysMs = new[] { 400, 800, 1200, 2000, 3000 };

        for (int attempt = 0; attempt < delaysMs.Length; attempt++)
        {
            using var resp = await SendXnatGetAsync(url, baseUrl, ct, authToken);

            if ((int)resp.StatusCode == 202)
            {
                var headers = string.Join(" | ", resp.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"));
                _logger.LogWarning(
                    "XNAT {Key} ({Name}) still 202 (attempt {Attempt}). URL={Url}. Headers={Headers}",
                    instKey, instName, attempt + 1, url, headers);

                if (resp.Headers.TryGetValues("Retry-After", out var vals) &&
                    int.TryParse(vals.FirstOrDefault(), out var retryAfterSeconds) &&
                    retryAfterSeconds > 0 && retryAfterSeconds <= 30)
                {
                    await Task.Delay(TimeSpan.FromSeconds(retryAfterSeconds), ct);
                }
                else
                {
                    await Task.Delay(delaysMs[attempt], ct);
                }

                continue;
            }

            if (resp.Headers.TryGetValues("x-amzn-waf-action", out var waf) &&
                waf.Any(v => v.Equals("challenge", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("XNAT {Key} ({Name}) blocked by AWS WAF challenge. URL={Url}", instKey, instName, url);
                return null;
            }

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "XNAT {Key} ({Name}) retry failed. Status={Status}. URL={Url}. Body(first 500)={Body}",
                    instKey, instName, (int)resp.StatusCode, url,
                    err.Length > 500 ? err[..500] : err);
                return null;
            }

            return await resp.Content.ReadAsStringAsync(ct);
        }

        _logger.LogWarning(
            "XNAT {Key} ({Name}) kept returning 202 after retries. URL={Url}",
            instKey, instName, url);

        return null;
    }

    // ---- Poll a given URL until it stops returning 202 ----
    private async Task<HttpResponseMessage> PollUrlUntilNot202Async(
        string pollUrl,
        string baseUrl,
        string instKey,
        string instName,
        CancellationToken ct,
        string? authToken = null)
    {
        var delaysMs = new[] { 300, 600, 1000, 1500, 2000 };

        for (int attempt = 0; attempt < delaysMs.Length; attempt++)
        {
            var resp = await SendXnatGetAsync(pollUrl, baseUrl, ct, authToken);

            if ((int)resp.StatusCode != 202)
                return resp;

            resp.Dispose();
            await Task.Delay(delaysMs[attempt], ct);
        }

        return await SendXnatGetAsync(pollUrl, baseUrl, ct, authToken);
    }

    // ---- HTTP GET helper with headers to survive strict gateways ----
    private Task<HttpResponseMessage> SendXnatGetAsync(
        string url,
        string baseUrl,
        CancellationToken ct,
        string? authToken = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 PIDAR/1.0");
        req.Headers.TryAddWithoutValidation("Accept", "application/json,text/plain,*/*");
        req.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        req.Headers.TryAddWithoutValidation("Referer", baseUrl + "/");

        if (!string.IsNullOrWhiteSpace(authToken))
            req.Headers.TryAddWithoutValidation("Authorization", $"Basic {authToken}");

        return _http.SendAsync(req, ct);
    }
}