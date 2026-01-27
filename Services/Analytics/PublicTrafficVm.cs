namespace Pidar.Services.Analytics;

public sealed record DailyVisitPoint(string Date, int Visits);
public sealed record CountryPoint(string Country, int Visits);

public sealed class PublicTrafficVm
{
    public bool Enabled { get; set; }

    public int VisitsLast30 { get; set; }
    public int UniquesLast30 { get; set; }
    public List<DailyVisitPoint> VisitsPerDayLast30 { get; set; } = new();
    public List<CountryPoint> TopCountriesLast30 { get; set; } = new();
}
