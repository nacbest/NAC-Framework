using Microsoft.AspNetCore.Http;

namespace Nac.MultiTenancy.Resolution;

/// <summary>
/// Resolves the tenant identifier from the authenticated user's <c>tenant_id</c> claim.
/// </summary>
public sealed class ClaimTenantStrategy : ITenantResolutionStrategy
{
    public Task<string?> ResolveAsync(HttpContext context)
    {
        var value = context.User.FindFirst(NacTenantClaims.TenantId)?.Value;
        return Task.FromResult<string?>(string.IsNullOrWhiteSpace(value) ? null : value.Trim());
    }
}
