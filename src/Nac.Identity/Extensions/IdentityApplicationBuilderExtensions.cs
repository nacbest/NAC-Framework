using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Nac.Abstractions.Auth;
using Nac.Identity.CurrentUser;

namespace Nac.Identity.Extensions;

/// <summary>
/// Application builder extensions for NAC Identity.
/// </summary>
public static class IdentityApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the application to use NAC Identity.
    /// Adds middleware to preload permissions asynchronously.
    /// Should be called after UseAuthentication() and before UseAuthorization().
    /// </summary>
    public static IApplicationBuilder UseNacIdentity(this IApplicationBuilder app)
    {
        // Preload permissions async before handlers run (avoids sync-over-async in JwtCurrentUser)
        app.Use(async (context, next) =>
        {
            var currentUser = context.RequestServices.GetService(typeof(ICurrentUser));
            if (currentUser is JwtCurrentUser jwtUser && jwtUser.IsAuthenticated)
            {
                await jwtUser.LoadPermissionsAsync(context.RequestAborted);
            }
            await next();
        });

        return app;
    }
}
