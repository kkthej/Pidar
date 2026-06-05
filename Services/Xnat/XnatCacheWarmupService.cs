using Pidar.Services.Xnat;

namespace Pidar.Services.Xnat;

public sealed class XnatCacheWarmupService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<XnatCacheWarmupService> _logger;

    public XnatCacheWarmupService(IServiceProvider sp, ILogger<XnatCacheWarmupService> logger)
        => (_sp, _logger) = (sp, logger);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Warm up immediately on startup
        await WarmAsync(ct);

        // Then refresh every 9 minutes (just before the 10-minute cache expiry)
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(9));
        while (await timer.WaitForNextTickAsync(ct))
            await WarmAsync(ct);
    }

    private async Task WarmAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _sp.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IXnatMultiService>();
            await svc.GetAllPublicProjectsAsync(ct);
            _logger.LogInformation("XNAT cache warmed up successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "XNAT cache warmup failed.");
        }
    }
}