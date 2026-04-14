using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Nac.Identity.Seeding;

namespace Nac.Identity.Extensions;

/// <summary>
/// Application builder extensions for NAC Identity.
/// </summary>
public static class IdentityApplicationBuilderExtensions
{
    /// <summary>
    /// Configures the application to use NAC Identity.
    /// Should be called after UseAuthentication() and before UseAuthorization().
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="seedRoles">Whether to seed default roles on startup. Default: true.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseNacIdentity(
        this IApplicationBuilder app,
        bool seedRoles = true)
    {
        if (seedRoles)
        {
            // Run seeding synchronously during startup
            using var scope = app.ApplicationServices.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
            seeder.SeedDefaultRolesAsync().GetAwaiter().GetResult();
        }

        return app;
    }
}
