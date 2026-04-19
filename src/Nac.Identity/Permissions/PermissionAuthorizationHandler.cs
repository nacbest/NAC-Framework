using Microsoft.AspNetCore.Authorization;
using Nac.Core.Abstractions.Permissions;

namespace Nac.Identity.Permissions;

/// <summary>
/// ASP.NET Core authorization handler that fulfils a <see cref="PermissionRequirement"/>
/// by delegating to <see cref="IPermissionChecker"/>.
/// </summary>
internal sealed class PermissionAuthorizationHandler(IPermissionChecker permissionChecker)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (await permissionChecker.IsGrantedAsync(requirement.PermissionName))
            context.Succeed(requirement);
    }
}
