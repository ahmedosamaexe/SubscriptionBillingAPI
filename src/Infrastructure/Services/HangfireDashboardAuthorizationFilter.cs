using Hangfire.Dashboard;

namespace SubscriptionBillingAPI.Infrastructure.Services;

/// <summary>
/// Custom Hangfire Dashboard authorization filter.
/// Only allows authenticated users with the "Admin" role to access the dashboard.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Must be authenticated
        if (httpContext.User.Identity is not { IsAuthenticated: true })
            return false;

        // Must be in Admin role
        return httpContext.User.IsInRole("Admin");
    }
}
