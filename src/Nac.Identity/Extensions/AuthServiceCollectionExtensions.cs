using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Nac.Identity.Endpoints;

namespace Nac.Identity.Extensions;

/// <summary>
/// Extension methods for registering NAC Auth HTTP middleware into the DI container.
/// Call <see cref="AddNacAuthHttp"/> during host startup to enable
/// <see cref="TenantRequiredGateMiddleware"/> in the pipeline.
/// </summary>
public static class AuthServiceCollectionExtensions
{
    /// <summary>
    /// Registers middleware-related services required by the NAC auth HTTP surface.
    /// The host must also call <c>app.UseMiddleware&lt;TenantRequiredGateMiddleware&gt;()</c>
    /// (or <c>app.UseNacAuthGate()</c>) after authentication and tenant resolution middleware.
    /// </summary>
    public static IServiceCollection AddNacAuthHttp(this IServiceCollection services)
    {
        // TenantRequiredGateMiddleware is instantiated by the middleware pipeline;
        // no explicit DI registration needed for the middleware itself.
        // This method is the extension point for future per-request auth HTTP services.
        return services;
    }

    /// <summary>
    /// Adds <see cref="TenantRequiredGateMiddleware"/> to the request pipeline.
    /// Must be placed after <c>UseAuthentication()</c> and <c>UseNacMultiTenancy()</c>,
    /// and before <c>UseAuthorization()</c>.
    /// </summary>
    public static IApplicationBuilder UseNacAuthGate(this IApplicationBuilder app)
        => app.UseMiddleware<TenantRequiredGateMiddleware>();
}
