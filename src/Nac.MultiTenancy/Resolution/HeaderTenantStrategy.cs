using Microsoft.AspNetCore.Http;

namespace Nac.MultiTenancy.Resolution;

/// <summary>
/// Resolves the tenant identifier from the <c>X-Tenant-Id</c> HTTP request header.
/// </summary>
public sealed class HeaderTenantStrategy : ITenantResolutionStrategy
{
    public Task<string?> ResolveAsync(HttpContext context)
    {
        var value = context.Request.Headers[NacTenantHeaders.TenantId].FirstOrDefault();
        return Task.FromResult<string?>(string.IsNullOrWhiteSpace(value) ? null : value.Trim());
    }
}
