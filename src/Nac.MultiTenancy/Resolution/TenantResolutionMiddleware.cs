using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nac.MultiTenancy.Abstractions;

namespace Nac.MultiTenancy.Resolution;

/// <summary>
/// ASP.NET Core middleware that resolves the current tenant early in the pipeline.
/// Iterates registered <see cref="ITenantResolutionStrategy"/> instances in order;
/// first non-null result is used to load the tenant from <see cref="ITenantStore"/>
/// and populate <see cref="ITenantContext"/>.
/// </summary>
internal sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    // Strategies, store, and context are resolved per-request via InvokeAsync
    // method injection to avoid captive dependency (scoped in singleton).
    public async Task InvokeAsync(
        HttpContext httpContext,
        IEnumerable<ITenantResolutionStrategy> strategies,
        ITenantStore tenantStore,
        ITenantContext tenantContext,
        ILogger<TenantResolutionMiddleware> logger)
    {
        string? tenantId = null;

        foreach (var strategy in strategies)
        {
            tenantId = await strategy.ResolveAsync(httpContext);
            if (tenantId is not null) break;
        }

        if (tenantId is not null)
        {
            var tenant = await tenantStore.GetByIdAsync(tenantId, httpContext.RequestAborted);
            if (tenant is not null && tenant.IsActive)
                tenantContext.SetCurrentTenant(tenant);
            else
                logger.LogWarning("Tenant '{TenantId}' not found or inactive", tenantId);
        }

        await next(httpContext);
    }
}
