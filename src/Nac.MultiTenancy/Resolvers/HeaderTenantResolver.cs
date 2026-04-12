using Microsoft.AspNetCore.Http;
using Nac.Abstractions.MultiTenancy;

namespace Nac.MultiTenancy.Resolvers;

/// <summary>Resolves tenant from the <c>X-Tenant-ID</c> HTTP header.</summary>
public sealed class HeaderTenantResolver : ITenantResolver
{
    private readonly string _headerName;

    public HeaderTenantResolver(string headerName = "X-Tenant-ID")
        => _headerName = headerName;

    public Task<string?> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        context.Request.Headers.TryGetValue(_headerName, out var value);
        return Task.FromResult(value.FirstOrDefault());
    }
}

/// <summary>Resolves tenant from a JWT claim (e.g., <c>tenant_id</c>).</summary>
public sealed class ClaimTenantResolver : ITenantResolver
{
    private readonly string _claimType;

    public ClaimTenantResolver(string claimType = "tenant_id")
        => _claimType = claimType;

    public Task<string?> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        var value = context.User?.FindFirst(_claimType)?.Value;
        return Task.FromResult(value);
    }
}

/// <summary>Resolves tenant from the first subdomain (e.g., <c>acme.myapp.com</c> → <c>acme</c>).</summary>
public sealed class SubdomainTenantResolver : ITenantResolver
{
    public Task<string?> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        var host = context.Request.Host.Host;
        var parts = host.Split('.');

        // Need at least 3 parts: subdomain.domain.tld
        var tenantId = parts.Length >= 3 ? parts[0] : null;
        return Task.FromResult(tenantId);
    }
}

/// <summary>Resolves tenant from a <c>tenant</c> query string parameter.</summary>
public sealed class QueryStringTenantResolver : ITenantResolver
{
    private readonly string _parameterName;

    public QueryStringTenantResolver(string parameterName = "tenant")
        => _parameterName = parameterName;

    public Task<string?> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        var value = context.Request.Query[_parameterName].FirstOrDefault();
        return Task.FromResult(value);
    }
}
