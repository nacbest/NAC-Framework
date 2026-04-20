using Microsoft.AspNetCore.Http;
using Nac.Identity.Services;

namespace Nac.Identity.Endpoints;

/// <summary>
/// Middleware that enforces a resolved tenant context for authenticated requests unless
/// the endpoint is marked with <see cref="AllowTenantlessAttribute"/>.
/// Tenant presence is determined by the <c>tenant_id</c> claim in the JWT
/// (<see cref="NacIdentityClaims.TenantId"/>), which is populated after the JWT Bearer
/// authentication middleware runs and (if configured) after TenantResolutionMiddleware.
/// <para>
/// For impersonation tokens (identified by the RFC 8693 <c>act</c> claim), this middleware
/// also enforces tenant pinning: the effective tenant resolved from the JWT
/// <c>tenant_id</c> claim MUST match any <c>X-Tenant-Id</c> header present in the request.
/// Mismatches are rejected with 403 (<c>NAC_IMPERSONATION_TENANT_MISMATCH</c>) before
/// further processing — this prevents header-based tenant override when acting under an
/// impersonation token. Non-impersonation requests (no <c>act</c> claim) are unaffected.
/// </para>
/// Position in pipeline: after UseAuthentication(), before UseAuthorization().
/// </summary>
public sealed class TenantRequiredGateMiddleware(RequestDelegate next)
{
    /// <summary>Header name used by multi-tenancy strategies to override the current tenant.</summary>
    private const string TenantHeader = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Only enforce for authenticated requests.
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            // ── Impersonation tenant-pin check (H1) ──────────────────────────
            // When an impersonation token (RFC 8693 `act` claim) is present, the
            // tenant MUST match the pinned `tenant_id` claim.  Any X-Tenant-Id header
            // attempting to override the pinned tenant is rejected with 403.
            if (httpContext.User.FindFirst(NacIdentityClaims.ActClaim) is not null)
            {
                var pinnedTenantId = httpContext.User.FindFirst(NacIdentityClaims.TenantId)?.Value;
                if (httpContext.Request.Headers.TryGetValue(TenantHeader, out var headerTenant)
                    && !string.IsNullOrWhiteSpace(headerTenant)
                    && !string.Equals(headerTenant, pinnedTenantId, StringComparison.OrdinalIgnoreCase))
                {
                    httpContext.Response.ContentType = "application/problem+json";
                    httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await httpContext.Response.WriteAsJsonAsync(new
                    {
                        type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                        title = "Tenant override rejected for impersonation token.",
                        status = 403,
                        extensions = new { code = "NAC_IMPERSONATION_TENANT_MISMATCH" }
                    });
                    return;
                }
            }

            // ── Standard tenant-required gate ────────────────────────────────
            var allowTenantless = httpContext.GetEndpoint()
                ?.Metadata.GetMetadata<AllowTenantlessAttribute>();

            if (allowTenantless is null)
            {
                var tenantId = httpContext.User.FindFirst(NacIdentityClaims.TenantId)?.Value;
                if (string.IsNullOrWhiteSpace(tenantId))
                {
                    httpContext.Response.ContentType = "application/problem+json";
                    httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await httpContext.Response.WriteAsJsonAsync(new
                    {
                        type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                        title = "Tenant required.",
                        status = 403,
                        extensions = new { code = "NAC_TENANT_REQUIRED" }
                    });
                    return;
                }
            }
        }

        await next(httpContext);
    }
}
