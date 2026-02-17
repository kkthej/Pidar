using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Pidar.Services.Analytics;

public sealed class MatomoAnalyticsService : IAnalyticsService
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly MatomoOptions _opt;
    private readonly ILogger<MatomoAnalyticsService> _logger;
    private readonly IMemoryCache _cache;

    // Cache keys
    private const string CacheKey = "matomo:publicTraffic:last30";

    // Sensible defaults for low/medium traffic research portals
    private const string DefaultDate = "last30";
    private const string DefaultRangePeriod = "range";
    private const string DefaultDayPeriod = "day";
    private const int DefaultCountryLimit = 10;

    // Cache behavior
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);      // fresh
    private static readonly TimeSpan StaleTtl = TimeSpan.FromMinutes(60);     // acceptable stale fallback

    // Matomo returns: [{ "label": "Italy", "nb_visits": 10, ... }, ...]
    private sealed class MatomoCountryRow
    {
        public string? label { get; set; }
        public int nb_visits { get; set; }
    }

    public MatomoAnalyticsService(
        HttpClient http,
        IOptions<MatomoOptions> opt,
        ILogger<MatomoAnalyticsService> logger,
        IMemoryCache cache)
    {
        _http = http;
        _opt = opt.Value;
        _logger = logger;
        _cache = cache;
    }

    public async Task<PublicTrafficVm> GetPublicTrafficAsync(CancellationToken ct = default)
    {
        // If not configured, don't pretend it is.
        if (!IsConfigured())
        {
            _logger.LogInformation("Matomo analytics not configured (BaseUrl/SiteId/TokenAuth missing).");
            return new PublicTrafficVm { Enabled = false };
        }

        // Try fresh cache first
        if (_cache.TryGetValue(CacheKey, out PublicTrafficVm? cached) && cached is not null)
            return cached;

        // Otherwise fetch; if fetch fails, fall back to stale cached value if available.
        try
        {
            var vm = await FetchPublicTrafficLast30Async(ct);

            _cache.Set(CacheKey, vm, CacheTtl);
            // also keep a stale copy (separate key) to survive transient outages
            _cache.Set(CacheKey + ":stale", vm, StaleTtl);

            return vm;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matomo analytics fetch failed.");

            if (_cache.TryGetValue(CacheKey + ":stale", out PublicTrafficVm? stale) && stale is not null)
            {
                _logger.LogInformation("Serving stale Matomo analytics from cache.");
                return stale;
            }

            // Important: Enabled=true (configured) but no data (temporarily unavailable)
            return new PublicTrafficVm { Enabled = true };
        }
    }

    private async Task<PublicTrafficVm> FetchPublicTrafficLast30Async(CancellationToken ct)
    {
        // Do calls in parallel (faster page)
        var visitsTask = CallApiScalarIntAsync("VisitsSummary.getVisits", DefaultRangePeriod, DefaultDate, ct);
        var uniquesTask = CallApiScalarIntSafeAsync("VisitsSummary.getUniqueVisitors", DefaultRangePeriod, DefaultDate, ct);
        var dailyTask = CallApiDailySeriesAsync("VisitsSummary.getVisits", DefaultDayPeriod, DefaultDate, ct);
        var countriesTask = CallApiAsync<List<MatomoCountryRow>>(
            "UserCountry.getCountry",
            DefaultRangePeriod,
            DefaultDate,
            ct,
            extra: $"filter_limit={DefaultCountryLimit}");

        await Task.WhenAll(visitsTask, uniquesTask, dailyTask, countriesTask);

        var visitsRange = visitsTask.Result;
        var uniquesRange = uniquesTask.Result; // safe fallback inside
        var visitsDaily = dailyTask.Result;
        var countries = countriesTask.Result;

        return new PublicTrafficVm
        {
            Enabled = true,

            // KPI
            VisitsLast30 = visitsRange,
            UniquesLast30 = uniquesRange,

            // Series
            VisitsPerDayLast30 = visitsDaily
                .OrderBy(k => k.Key) // yyyy-mm-dd sorts lexicographically
                .Select(k => new DailyVisitPoint(k.Key, k.Value))
                .ToList(),

            // Breakdown
            TopCountriesLast30 = countries
                .Select(x => new CountryPoint(
                    Country: string.IsNullOrWhiteSpace(x.label) ? "Unknown" : x.label!,
                    Visits: x.nb_visits))
                .Where(x => x.Visits > 0)
                .ToList()
        };
    }

    private bool IsConfigured()
        => !string.IsNullOrWhiteSpace(_opt.BaseUrl)
           && !string.IsNullOrWhiteSpace(_opt.SiteId)
           && !string.IsNullOrWhiteSpace(_opt.TokenAuth)
           && !_opt.TokenAuth.Equals("PUT_TOKEN_HERE", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Matomo token_auth must be sent in POST body (your instance requires this).
    /// </summary>
    private async Task<JsonElement> PostMatomoAsync(string method, string period, string date, CancellationToken ct, string extra = "")
    {
        var url = BuildUrl(method, period, date, extra);

        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token_auth", _opt.TokenAuth ?? string.Empty)
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

        // Optional: short timeout to prevent hanging the page
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);

        if (resp.StatusCode == HttpStatusCode.Forbidden || resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Matomo auth/permission error (HTTP {(int)resp.StatusCode}). Body: {body}");
        }

        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement.Clone(); // clone because doc will be disposed

        // Matomo error payload: { "result":"error", "message":"..." }
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("result", out var r) &&
            r.ValueKind == JsonValueKind.String &&
            string.Equals(r.GetString(), "error", StringComparison.OrdinalIgnoreCase))
        {
            var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "Unknown Matomo error";
            throw new InvalidOperationException($"Matomo API error: {msg}");
        }

        return root;
    }

    private string BuildUrl(string method, string period, string date, string extra)
    {
        // BaseUrl can be "http://matomo" (docker service) or "https://pidar.../matomo"
        var baseUrl = _opt.BaseUrl!.TrimEnd('/');
        var url = $"{baseUrl}/index.php" +
                  $"?module=API&format=JSON" +
                  $"&idSite={Uri.EscapeDataString(_opt.SiteId!)}" +
                  $"&period={Uri.EscapeDataString(period)}" +
                  $"&date={Uri.EscapeDataString(date)}" +
                  $"&method={Uri.EscapeDataString(method)}";

        if (!string.IsNullOrWhiteSpace(extra))
            url += $"&{extra.TrimStart('&')}";

        return url;
    }

    private async Task<T> CallApiAsync<T>(string method, string period, string date, CancellationToken ct, string extra = "")
    {
        var root = await PostMatomoAsync(method, period, date, ct, extra);

        return JsonSerializer.Deserialize<T>(root.GetRawText(), JsonOpts)
               ?? throw new InvalidOperationException("Matomo returned empty response.");
    }

    /// <summary>
    /// Matomo scalar endpoints may return:
    /// - 123
    /// - { "value": 123 } or { "value": "123" }
    /// </summary>
    private async Task<int> CallApiScalarIntAsync(string method, string period, string date, CancellationToken ct, string extra = "")
    {
        var root = await PostMatomoAsync(method, period, date, ct, extra);

        // raw number
        if (root.ValueKind == JsonValueKind.Number && root.TryGetInt32(out var n))
            return n;

        // { "value": ... }
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("value", out var v))
        {
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var vn)) return vn;
            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var vs)) return vs;
        }

        throw new InvalidOperationException($"Unexpected Matomo scalar response: {root.GetRawText()}");
    }

    /// <summary>
    /// Unique visitors may not be supported for certain period configs on some Matomo setups.
    /// This method will never throw; it returns 0 on failure and logs at Information.
    /// </summary>
    private async Task<int> CallApiScalarIntSafeAsync(string method, string period, string date, CancellationToken ct, string extra = "")
    {
        try
        {
            return await CallApiScalarIntAsync(method, period, date, ct, extra);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Matomo scalar '{Method}' not available for period={Period} date={Date}. Returning 0.",
                method, period, date);
            return 0;
        }
    }

    /// <summary>
    /// Daily series endpoints typically return:
    /// { "2026-02-16": 17, "2026-02-15": 0, ... }
    /// </summary>
    private async Task<Dictionary<string, int>> CallApiDailySeriesAsync(string method, string period, string date, CancellationToken ct, string extra = "")
    {
        var root = await PostMatomoAsync(method, period, date, ct, extra);

        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"Unexpected Matomo daily-series response: {root.GetRawText()}");

        var dict = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var prop in root.EnumerateObject())
        {
            var key = prop.Name;
            var val = prop.Value;

            if (val.ValueKind == JsonValueKind.Number && val.TryGetInt32(out var n))
                dict[key] = n;
            else if (val.ValueKind == JsonValueKind.String && int.TryParse(val.GetString(), out var s))
                dict[key] = s;
            else
                dict[key] = 0; // defensive default
        }

        return dict;
    }
}