using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nac.Core.MultiTenancy;

namespace Nac.MultiTenancy;

/// <summary>
/// ASP.NET Core middleware that resolves the current tenant from the HTTP request.
/// Runs the registered <see cref="ITenantResolver"/> chain (first match wins),
/// then looks up full <see cref="TenantInfo"/> via <see cref="ITenantStore"/>,
/// and sets the scoped <see cref="ITenantContext"/>.
/// </summary>
internal sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        IEnumerable<ITenantResolver> resolvers,
        ITenantStore store,
        ITenantContext tenantContext)
    {
        // Chain resolvers — first non-null result wins
        string? tenantId = null;
        foreach (var resolver in resolvers)
        {
            tenantId = await resolver.ResolveAsync(httpContext, httpContext.RequestAborted);
            if (tenantId is not null)
                break;
        }

        if (tenantId is null)
        {
            _logger.LogDebug("No tenant resolved for {Path}", httpContext.Request.Path);
            await _next(httpContext);
            return;
        }

        var tenantInfo = await store.GetByIdAsync(tenantId, httpContext.RequestAborted);
        if (tenantInfo is null)
        {
            _logger.LogWarning("Tenant '{TenantId}' not found in store", tenantId);
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new { error = $"Unknown tenant: {tenantId}" });
            return;
        }

        // Set the scoped tenant context
        if (tenantContext is TenantContext mutableContext)
            mutableContext.Current = tenantInfo;

        _logger.LogDebug("Resolved tenant {TenantId} ({TenantName})", tenantInfo.Id, tenantInfo.Name);

        await _next(httpContext);
    }
}
