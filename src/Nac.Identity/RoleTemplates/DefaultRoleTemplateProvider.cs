namespace Nac.Identity.RoleTemplates;

/// <summary>
/// Ships the four built-in system role templates: <c>owner</c>, <c>admin</c>,
/// <c>member</c>, and <c>guest</c>. Phase 05 will formally define permission names
/// via <c>IPermissionDefinitionProvider</c>; these strings are the canonical names
/// used by both the seeder and the permission definition provider.
/// </summary>
public sealed class DefaultRoleTemplateProvider : IRoleTemplateProvider
{
    // ── Canonical permission name constants ───────────────────────────────────

    private const string UsersView      = "Identity.Management.Users.View";
    private const string UsersManage    = "Identity.Management.Users.Manage";
    private const string MembersView    = "Identity.Management.Memberships.View";
    private const string MembersManage  = "Identity.Management.Memberships.Manage";
    private const string RolesView      = "Identity.Management.Roles.View";
    private const string RolesManage    = "Identity.Management.Roles.Manage";
    private const string PermsManage    = "Identity.Management.Permissions.Manage";
    private const string TenantsManage  = "Identity.Management.Tenants.Manage";

    /// <inheritdoc/>
    public void Define(IRoleTemplateContext context)
    {
        // owner — full control: all Identity.Management permissions + tenant-level admin.
        context.AddTemplate("owner", "Owner", "Full tenant ownership with all management permissions.")
            .Grants(
                UsersView, UsersManage,
                MembersView, MembersManage,
                RolesView, RolesManage,
                PermsManage,
                TenantsManage);

        // admin — user, membership, and role management; cannot manage permissions or tenant.
        context.AddTemplate("admin", "Admin", "Tenant administrator: manages users, memberships, and roles.")
            .Grants(
                UsersView, UsersManage,
                MembersView, MembersManage,
                RolesView, RolesManage);

        // member — read-only access to users and their own membership.
        context.AddTemplate("member", "Member", "Standard tenant member with read-only visibility.")
            .Grants(
                UsersView,
                MembersView);

        // guest — no permissions; host application extends as needed.
        context.AddTemplate("guest", "Guest", "No default permissions. Host application extends as needed.");
    }
}
