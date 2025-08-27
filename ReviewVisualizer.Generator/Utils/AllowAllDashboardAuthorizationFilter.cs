using Hangfire.Dashboard;

namespace ReviewVisualizer.Generator.Utils
{
    public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context) => true;
    }
}
