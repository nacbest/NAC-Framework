using Microsoft.AspNetCore.Http;

namespace Nac.Abstractions.MultiTenancy;

/// <summary>
/// Resolves the current tenant from the HTTP request.
/// Multiple resolvers are chained (chain-of-responsibility): Header → Subdomain → Claim → Route → QueryString.
/// First resolver that returns a non-null result wins.
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Attempts to resolve a tenant identifier from the request.
    /// Returns null if this resolver cannot determine the tenant.
    /// </summary>
    Task<string?> ResolveAsync(HttpContext context, CancellationToken ct = default);
}
