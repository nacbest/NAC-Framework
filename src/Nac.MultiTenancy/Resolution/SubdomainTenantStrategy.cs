using Microsoft.AspNetCore.Http;
using System.Net;

namespace Nac.MultiTenancy.Resolution;

/// <summary>
/// Resolves the tenant identifier from the request subdomain.
/// Example: <c>tenant1.app.com</c> → <c>tenant1</c>.
/// Returns null for IP addresses, localhost, and single-label hosts.
/// </summary>
public sealed class SubdomainTenantStrategy : ITenantResolutionStrategy
{
    public Task<string?> ResolveAsync(HttpContext context)
    {
        var host = context.Request.Host.Host; // strips port

        // Skip IP addresses
        if (IPAddress.TryParse(host, out _))
            return Task.FromResult<string?>(null);

        // Skip localhost or single-label (no dots → no subdomain)
        var dotIndex = host.IndexOf('.');
        if (dotIndex <= 0)
            return Task.FromResult<string?>(null);

        var subdomain = host[..dotIndex].Trim();

        // Guard against empty segment (e.g. ".app.com")
        if (string.IsNullOrWhiteSpace(subdomain))
            return Task.FromResult<string?>(null);

        return Task.FromResult<string?>(subdomain);
    }
}
