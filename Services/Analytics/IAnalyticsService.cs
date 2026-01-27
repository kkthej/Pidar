namespace Pidar.Services.Analytics;

public interface IAnalyticsService
{
    Task<PublicTrafficVm> GetPublicTrafficAsync(CancellationToken ct = default);
}
