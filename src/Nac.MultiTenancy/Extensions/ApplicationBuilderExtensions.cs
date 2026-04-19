using Microsoft.AspNetCore.Builder;
using Nac.MultiTenancy.Resolution;

namespace Nac.MultiTenancy.Extensions;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> that wire up NAC multi-tenancy
/// middleware into the ASP.NET Core request pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the <see cref="TenantResolutionMiddleware"/> to the request pipeline.
    /// Must be called before any middleware that requires an active tenant context
    /// (e.g. authorization, controllers).
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseNacMultiTenancy(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantResolutionMiddleware>();
    }
}
