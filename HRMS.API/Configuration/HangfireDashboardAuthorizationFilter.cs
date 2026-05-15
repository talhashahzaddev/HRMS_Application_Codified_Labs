using Hangfire.Dashboard;

namespace HRMS.API.Configuration;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && (httpContext.User.IsInRole("Super Admin") || httpContext.User.IsInRole("HR Manager"));
    }
}
