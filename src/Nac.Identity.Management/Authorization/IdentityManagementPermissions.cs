namespace Nac.Identity.Management.Authorization;

/// <summary>
/// Named permission constants for the Identity Management module.
/// All values are registered as ASP.NET Core authorization policies by
/// <c>AddNacIdentityManagement</c> and evaluated via <c>IPermissionChecker</c>.
/// </summary>
public static class IdentityManagementPermissions
{
    public const string Users_View   = "Identity.Management.Users.View";
    public const string Users_Manage = "Identity.Management.Users.Manage";

    public const string Memberships_View   = "Identity.Management.Memberships.View";
    public const string Memberships_Manage = "Identity.Management.Memberships.Manage";

    public const string Roles_View   = "Identity.Management.Roles.View";
    public const string Roles_Manage = "Identity.Management.Roles.Manage";

    public const string Grants_View   = "Identity.Management.Grants.View";
    public const string Grants_Manage = "Identity.Management.Grants.Manage";

    public const string Permissions_View = "Identity.Management.Permissions.View";

    /// <summary>All permission names — used by <c>AddNacIdentityManagement</c> to register policies.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        Users_View, Users_Manage,
        Memberships_View, Memberships_Manage,
        Roles_View, Roles_Manage,
        Grants_View, Grants_Manage,
        Permissions_View,
    ];
}
