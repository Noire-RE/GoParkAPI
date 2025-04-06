using Hangfire.Dashboard;

namespace GoParkAPI.Services
{
    public class AllowAnonymousAuthorization : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true;
        }
    }
}
