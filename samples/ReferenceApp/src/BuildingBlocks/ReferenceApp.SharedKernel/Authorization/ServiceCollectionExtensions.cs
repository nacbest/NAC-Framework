using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace ReferenceApp.SharedKernel.Authorization;

/// <summary>
/// Registration helpers for SharedKernel authorization infrastructure.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the dynamic <see cref="PermissionAuthorizationPolicyProvider"/>.
    /// The matching <c>PermissionAuthorizationHandler</c> is already registered by
    /// <c>Nac.Identity</c> via <c>AddNacIdentity()</c> — do NOT register it here.
    /// </summary>
    public static IServiceCollection AddNacPermissionPolicies(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
        return services;
    }
}
