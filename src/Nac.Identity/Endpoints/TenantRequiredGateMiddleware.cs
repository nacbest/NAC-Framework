using Microsoft.AspNetCore.Http;
using Nac.Identity.Services;

namespace Nac.Identity.Endpoints;

/// <summary>
/// Middleware that enforces a resolved tenant context for authenticated requests unless
/// the endpoint is marked with <see cref="AllowTenantlessAttribute"/>.
/// Tenant presence is determined by the <c>tenant_id</c> claim in the JWT
/// (<see cref="NacIdentityClaims.TenantId"/>), which is populated after the JWT Bearer
/// authentication middleware runs and (if configured) after TenantResolutionMiddleware.
/// Position in pipeline: after UseAuthentication(), before UseAuthorization().
/// </summary>
public sealed class TenantRequiredGateMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Only enforce for authenticated requests.
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
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
