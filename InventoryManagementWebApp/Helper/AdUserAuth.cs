using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;

namespace InventoryManagementWebApp.Helpers
{
    public class AdUserAuth
    {
        private readonly RequestDelegate _next;

        public AdUserAuth(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var user = httpContext.User;
            var userName = user.Identity?.Name?.Split('\\')[1];

            if (string.IsNullOrEmpty(userName))
            {
                throw new Exception("User name not found in claims.");
            }

            using (PrincipalContext ctx = new PrincipalContext(ContextType.Domain))
            {
                UserPrincipal domainUser = UserPrincipal.FindByIdentity(ctx, userName);

                List<string> userRoles = new List<string>();

                httpContext.Session.SetString("SessionStarted", "true");

                if (domainUser != null && httpContext.Session.GetString("UserIdAuthorized") != "true")
                {
                    var groups = domainUser.GetGroups();

                    foreach (Principal principal in groups)
                    {
                        if (principal.Name == "jgufis saxeli")
                        {
                            httpContext.Session.SetString("UserGroup", principal.Name);
                            httpContext.Session.SetString("UserIdAuthorized", "true");
                        }
                    }
                }

                if (httpContext.Session.GetString("UserIdAuthorized") != "true")
                {
                    throw new Exception("User is not authorized for this service!");
                }

                await _next(httpContext);
            }
        }
    }

    public static class AdUserAuthExtensions
    {
        public static IApplicationBuilder UseAdUserAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AdUserAuth>();
        }
    }
}
