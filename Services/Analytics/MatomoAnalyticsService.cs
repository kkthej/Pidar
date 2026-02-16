using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Pidar.Services.Analytics;

public sealed class MatomoAnalyticsService : IAnalyticsService
{
    private readonly HttpClient _http;
    private readonly MatomoOptions _opt;
    private readonly ILogger<MatomoAnalyticsService> _logger;

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
            // range totals (Matomo sometimes returns { "value": ... } )
            var visitsRange = await CallApiIntAsync("VisitsSummary.getVisits", "range", "last30", ct);
            int uniquesRange;
            try
            {
                uniquesRange = await CallApiIntAsync("VisitsSummary.getUniqueVisitors", "range", "last30", ct);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Matomo unique visitors not available for this period. Falling back to 0.");
                uniquesRange = 0;
            }

            // day series: { "YYYY-MM-DD": 12, ... }
            var visitsDaily = await CallApiAsync<Dictionary<string, int>>(
                "VisitsSummary.getVisits", "day", "last30", ct);

            // countries list: [{ label: "Italy", nb_visits: 10 }, ...]
            // NOTE: extra should NOT start with '&' anymore (BuildUrl will add it)
            var countries = await CallApiAsync<List<Dictionary<string, object>>>(
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
                        Country: x.TryGetValue("label", out var c) ? c?.ToString() ?? "Unknown" : "Unknown",
                        Visits: x.TryGetValue("nb_visits", out var v) ? Convert.ToInt32(v) : 0))
                    .Where(x => x.Visits > 0)
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matomo analytics request failed. Returning empty stats.");
            return new PublicTrafficVm { Enabled = true };
        }
    }

    private bool IsConfigured()
        => !string.IsNullOrWhiteSpace(_opt.BaseUrl)
           && !string.IsNullOrWhiteSpace(_opt.SiteId)
           && !string.IsNullOrWhiteSpace(_opt.TokenAuth)
           && !_opt.TokenAuth.Equals("PUT_TOKEN_HERE", StringComparison.OrdinalIgnoreCase);

    // IMPORTANT: token_auth must NOT be in querystring
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

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("result", out var r) &&
            r.ValueKind == JsonValueKind.String &&
            string.Equals(r.GetString(), "error", StringComparison.OrdinalIgnoreCase))
        {
            var msg = root.TryGetProperty("message", out var m) ? m.GetString() : "Unknown Matomo error";
            throw new InvalidOperationException($"Matomo API error: {msg}");
        }

        if (root.ValueKind == JsonValueKind.Number && root.TryGetInt32(out var n))
            return n;

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