using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nac.Core.Abstractions.Identity;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Permissions.Host;

namespace Nac.Identity.Management.Authorization;

/// <summary>
/// Authorization filter that gates all impersonation endpoints on two conditions:
/// <list type="bullet">
///   <item><c>ICurrentUser.IsHost == true</c> — caller is a platform (host-realm) user.</item>
///   <item><c>IPermissionChecker.IsGrantedAsync(Host.ImpersonateTenant)</c> — runtime permission
///         check (Pattern A: permissions are NOT embedded in JWT).</item>
/// </list>
/// Both conditions are required — defense in depth.
/// Register as Scoped (depends on scoped <see cref="ICurrentUser"/>).
/// </summary>
internal sealed class HostImpersonationFilter(
    ICurrentUser currentUser,
    IPermissionChecker permissionChecker) : IAsyncAuthorizationFilter
{
    private static readonly object ForbidBody = new { code = "NAC_HOST_IMPERSONATION_REQUIRED" };

    /// <inheritdoc />
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!currentUser.IsAuthenticated || !currentUser.IsHost)
        {
            context.Result = new ObjectResult(ForbidBody) { StatusCode = 403 };
            return;
        }

        if (!await permissionChecker.IsGrantedAsync(
                HostPermissions.ImpersonateTenant,
                context.HttpContext.RequestAborted))
        {
            context.Result = new ObjectResult(ForbidBody) { StatusCode = 403 };
        }
    }
}
