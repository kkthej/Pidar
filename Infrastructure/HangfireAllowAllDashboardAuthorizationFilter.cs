using Hangfire.Dashboard;

namespace Pidar.Infrastructure;

public sealed class HangfireAllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
