using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Pidar.Services.Analytics;

public sealed class MatomoAnalyticsService : IAnalyticsService
{
    private readonly HttpClient _http;
    private readonly MatomoOptions _opt;
    private readonly ILogger<MatomoAnalyticsService> _logger;

    // Strongly-typed DTO for Matomo country API
    // Matomo returns: [{ "label": "Italy", "nb_visits": 10, ... }, ...]
    private sealed class MatomoCountryRow
    {
        public string? label { get; set; }
        public int nb_visits { get; set; }
    }

    public MatomoAnalyticsService(
        HttpClient http,
        IOptions<MatomoOptions> opt,
        ILogger<MatomoAnalyticsService> logger)
    {
        _http = http;
        _opt = opt.Value;
        _logger = logger;
    }

    public async Task<PublicTrafficVm> GetPublicTrafficAsync(CancellationToken ct = default)
    {
        if (!IsConfigured())
        {
            _logger.LogInformation(
                "Matomo analytics not configured yet (missing BaseUrl/SiteId/TokenAuth). Returning empty stats.");
            return new PublicTrafficVm { Enabled = false };
        }

        try
        {
            // Visits total (last 30)
            var visitsRange = await CallApiIntAsync("VisitsSummary.getVisits", "range", "last30", ct);

            // Unique visitors may be unsupported for range on some Matomo setups -> don't fail whole UI
            int uniquesRange;
            try
            {
                uniquesRange = await CallApiIntAsync("VisitsSummary.getUniqueVisitors", "range", "last30", ct);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex,
                    "Matomo unique visitors not available for this period. Falling back to 0.");
                uniquesRange = 0;
            }

            // Daily series (last 30)
            var visitsDaily = await CallApiAsync<Dictionary<string, int>>(
                "VisitsSummary.getVisits", "day", "last30", ct);

            // Top countries (last 30)
            var countries = await CallApiAsync<List<MatomoCountryRow>>(
                "UserCountry.getCountry", "range", "last30", ct, extra: "filter_limit=10");

            return new PublicTrafficVm
            {
                Enabled = true,
                VisitsLast30 = visitsRange,
                UniquesLast30 = uniquesRange,
                VisitsPerDayLast30 = visitsDaily
                    .OrderBy(k => k.Key)
                    .Select(k => new DailyVisitPoint(k.Key, k.Value))
                    .ToList(),
                TopCountriesLast30 = countries
                    .Select(x => new CountryPoint(
                        Country: string.IsNullOrWhiteSpace(x.label) ? "Unknown" : x.label!,
                        Visits: x.nb_visits))
                    .Where(x => x.Visits > 0)
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matomo analytics request failed. Returning empty stats.");
            // Keep Enabled=true so UI knows tracking is configured even if stats fail temporarily
            return new PublicTrafficVm { Enabled = true };
        }
    }

    private bool IsConfigured()
        => !string.IsNullOrWhiteSpace(_opt.BaseUrl)
           && !string.IsNullOrWhiteSpace(_opt.SiteId)
           && !string.IsNullOrWhiteSpace(_opt.TokenAuth)
           && !_opt.TokenAuth.Equals("PUT_TOKEN_HERE", StringComparison.OrdinalIgnoreCase);

    // IMPORTANT: token_auth must NOT be in querystring (your server requires POST body)
    private string BuildUrl(string method, string period, string date, string extra)
    {
        var baseUrl = _opt.BaseUrl.TrimEnd('/') + "/index.php";

        var url =
            $"{baseUrl}?module=API&format=JSON" +
            $"&idSite={Uri.EscapeDataString(_opt.SiteId)}" +
            $"&period={Uri.EscapeDataString(period)}" +
            $"&date={Uri.EscapeDataString(date)}" +
            $"&method={Uri.EscapeDataString(method)}";

        if (!string.IsNullOrWhiteSpace(extra))
            url += $"&{extra.TrimStart('&')}";

        return url;
    }

    private async Task<T> CallApiAsync<T>(string method, string period, string date, CancellationToken ct, string extra = "")
    {
        var url = BuildUrl(method, period, date, extra);

        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token_auth", _opt.TokenAuth ?? string.Empty)
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Matomo error object
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("result", out var r) &&
            r.ValueKind == JsonValueKind.String &&
            string.Equals(r.GetString(), "error", StringComparison.OrdinalIgnoreCase))
        {
            var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "Unknown Matomo error";
            throw new InvalidOperationException($"Matomo API error: {msg}");
        }

        return JsonSerializer.Deserialize<T>(root.GetRawText())
               ?? throw new InvalidOperationException("Matomo returned empty response.");
    }

    // Supports:
    // 1) 123
    // 2) { "value": 123 } or { "value": "123" }
    private async Task<int> CallApiIntAsync(string method, string period, string date, CancellationToken ct, string extra = "")
    {
        var url = BuildUrl(method, period, date, extra);

        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token_auth", _opt.TokenAuth ?? string.Empty)
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Matomo error object
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("result", out var r) &&
            r.ValueKind == JsonValueKind.String &&
            string.Equals(r.GetString(), "error", StringComparison.OrdinalIgnoreCase))
        {
            var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "Unknown Matomo error";
            throw new InvalidOperationException($"Matomo API error: {msg}");
        }

        // Case 1: raw number
        if (root.ValueKind == JsonValueKind.Number && root.TryGetInt32(out var n))
            return n;

        // Case 2: { "value": ... }
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("value", out var v))
        {
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var vn))
                return vn;

            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var vs))
                return vs;
        }

        throw new InvalidOperationException($"Unexpected Matomo scalar response: {json}");
    }
}