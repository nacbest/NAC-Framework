using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nac.Core.Abstractions.Identity;
using Nac.Core.Abstractions.Permissions;

namespace Nac.MultiTenancy.Management.Authorization;

/// <summary>
/// Rejects callers that are not host users with the <c>Host.AccessAllTenants</c> permission.
/// Both conditions required — defense in depth: IsHost flag + explicit permission grant.
/// </summary>
internal sealed class HostAdminOnlyFilter(ICurrentUser user, IPermissionChecker permissionChecker)
    : IAsyncActionFilter
{
    private static readonly object ForbidBody = new { code = "NAC_HOST_REQUIRED" };

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!user.IsAuthenticated || !user.IsHost)
        {
            context.Result = new ObjectResult(ForbidBody) { StatusCode = 403 };
            return;
        }

        if (!await permissionChecker.IsGrantedAsync("Host.AccessAllTenants",
                context.HttpContext.RequestAborted))
        {
            context.Result = new ObjectResult(ForbidBody) { StatusCode = 403 };
            return;
        }

        await next();
    }
}
