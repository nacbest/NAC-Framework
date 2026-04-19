using Microsoft.AspNetCore.Http;

namespace Nac.MultiTenancy.Resolution;

/// <summary>
/// Defines a strategy for resolving the current tenant identifier from an HTTP request.
/// Strategies are tried in registration order; first non-null result wins.
/// </summary>
public interface ITenantResolutionStrategy
{
    Task<string?> ResolveAsync(HttpContext context);
}
