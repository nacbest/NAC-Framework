using Microsoft.AspNetCore.Http;

namespace Nac.MultiTenancy.Resolution;

/// <summary>
/// Resolves the tenant identifier from the <c>{tenantId}</c> route parameter.
/// </summary>
public sealed class RouteTenantStrategy : ITenantResolutionStrategy
{
    public Task<string?> ResolveAsync(HttpContext context)
    {
        var value = context.Request.RouteValues["tenantId"]?.ToString();
        return Task.FromResult<string?>(string.IsNullOrWhiteSpace(value) ? null : value.Trim());
    }
}
