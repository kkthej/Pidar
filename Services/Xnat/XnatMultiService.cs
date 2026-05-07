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
        return await _cache.GetOrCreateAsync("xnat_all_public_projects_v4", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

            var instances = _config.GetSection("XnatInstances").Get<List<XnatInstanceOptions>>() ?? new();
            var results = new List<XnatPublicProject>();

            foreach (var inst in instances.Where(x => !string.IsNullOrWhiteSpace(x.BaseUrl)))
            {
                var baseUrl = inst.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/data/projects?format=json";

                try
                {
                    var json = await GetJsonHandling202Async(url, baseUrl, inst.Key, inst.Name, ct);
                    if (string.IsNullOrWhiteSpace(json))
                        continue;

                    var parsed = JsonSerializer.Deserialize<XnatProjectsApiResponse>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var rows = parsed?.ResultSet?.Result ?? new();

                    foreach (var p in rows)
                    {
                        if (string.IsNullOrWhiteSpace(p.ID)) continue;

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
            }

            return (IReadOnlyList<XnatPublicProject>)results
                .OrderBy(x => x.InstanceName)
                .ThenBy(x => x.Id)
                .ToList();

        }) ?? Array.Empty<XnatPublicProject>();
    }

    // ---- Core logic: GET JSON with robust 202 handling ----
    private async Task<string?> GetJsonHandling202Async(
        string url,
        string baseUrl,
        string instKey,
        string instName,
        CancellationToken ct)
    {
        // First request
        using var firstResp = await SendXnatGetAsync(url, baseUrl, ct);

        if ((int)firstResp.StatusCode == 202)
        {
            // If Location exists, poll Location
            var location = firstResp.Headers.Location?.ToString();

            if (!string.IsNullOrWhiteSpace(location))
            {
                if (location.StartsWith("/"))
                    location = baseUrl + location;

                _logger.LogInformation(
                    "XNAT {Key} ({Name}) returned 202 with Location. Polling Location={Location}",
                    instKey, instName, location);

                using var finalResp = await PollUrlUntilNot202Async(location, baseUrl, instKey, instName, ct);
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

            // No Location: retry the same URL with backoff
            _logger.LogWarning(
                "XNAT {Key} ({Name}) returned 202 but no Location header. Will retry same URL with backoff. URL={Url}",
                instKey, instName, url);

            // We already got one 202; now retry url a few times
            return await RetrySameUrlUntil200Async(url, baseUrl, instKey, instName, ct);
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
        CancellationToken ct)
    {
        var delaysMs = new[] { 400, 800, 1200, 2000, 3000 };

        for (int attempt = 0; attempt < delaysMs.Length; attempt++)
        {
            using var resp = await SendXnatGetAsync(url, baseUrl, ct);

            if ((int)resp.StatusCode == 202)
            {
                var headers = string.Join(" | ", resp.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"));
                _logger.LogWarning(
                    "XNAT {Key} ({Name}) still 202 (attempt {Attempt}). URL={Url}. Headers={Headers}",
                    instKey, instName, attempt + 1, url, headers);

                // Prefer Retry-After if present
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
        CancellationToken ct)
    {
        var delaysMs = new[] { 300, 600, 1000, 1500, 2000 };

        for (int attempt = 0; attempt < delaysMs.Length; attempt++)
        {
            var resp = await SendXnatGetAsync(pollUrl, baseUrl, ct);

            if ((int)resp.StatusCode != 202)
                return resp; // caller disposes

            resp.Dispose();
            await Task.Delay(delaysMs[attempt], ct);
        }

        // last attempt
        return await SendXnatGetAsync(pollUrl, baseUrl, ct);
    }

    // ---- HTTP GET helper with headers to survive strict gateways ----
    private Task<HttpResponseMessage> SendXnatGetAsync(string url, string baseUrl, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 PIDAR/1.0");
        req.Headers.TryAddWithoutValidation("Accept", "application/json,text/plain,*/*");
        req.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        req.Headers.TryAddWithoutValidation("Referer", baseUrl + "/");
        return _http.SendAsync(req, ct);
    }
}