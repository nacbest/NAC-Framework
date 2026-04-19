using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Nac.Identity.Permissions;

namespace ReferenceApp.SharedKernel.Authorization;

/// <summary>
/// Dynamic authorization policy provider (Milan Jovanović pattern).
/// Intercepts any unknown policy name and builds an <see cref="AuthorizationPolicy"/>
/// containing a <see cref="PermissionRequirement"/> from Nac.Identity.
/// Built-in policies (e.g. "Bearer") fall through to <see cref="DefaultAuthorizationPolicyProvider"/>.
/// Note: <see cref="Nac.Identity.Permissions.PermissionAuthorizationHandler"/> is registered
/// by Nac.Identity — this provider only supplies the policy; no handler duplication.
/// </summary>
internal sealed class PermissionAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Let built-in / explicitly registered policies resolve first
        var existingPolicy = await _fallback.GetPolicyAsync(policyName);
        if (existingPolicy is not null)
            return existingPolicy;

        // Treat any unknown policy name as a permission name
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();

        return policy;
    }
}
