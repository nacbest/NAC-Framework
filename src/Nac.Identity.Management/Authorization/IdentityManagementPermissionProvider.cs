using Nac.Core.Abstractions.Permissions;

namespace Nac.Identity.Management.Authorization;

/// <summary>
/// Registers all Identity Management permissions under the <c>Identity.Management</c>
/// group so they appear in the permission tree endpoint and are evaluated by
/// <see cref="Nac.Identity.Permissions.PermissionDefinitionManager"/>.
/// </summary>
public sealed class IdentityManagementPermissionProvider : IPermissionDefinitionProvider
{
    public void Define(IPermissionDefinitionContext context)
    {
        var group = context.AddGroup("Identity.Management", "Identity Management");

        var users = group.AddPermission(IdentityManagementPermissions.Users_View, "View Users");
        users.AddChild(IdentityManagementPermissions.Users_Manage, "Manage Users");

        var memberships = group.AddPermission(IdentityManagementPermissions.Memberships_View, "View Memberships");
        memberships.AddChild(IdentityManagementPermissions.Memberships_Manage, "Manage Memberships");

        var roles = group.AddPermission(IdentityManagementPermissions.Roles_View, "View Roles");
        roles.AddChild(IdentityManagementPermissions.Roles_Manage, "Manage Roles");

        var grants = group.AddPermission(IdentityManagementPermissions.Grants_View, "View Grants");
        grants.AddChild(IdentityManagementPermissions.Grants_Manage, "Manage Grants");

        group.AddPermission(IdentityManagementPermissions.Permissions_View, "View Permission Tree");
    }
}
