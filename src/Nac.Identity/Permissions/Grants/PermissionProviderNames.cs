namespace Nac.Identity.Permissions.Grants;

/// <summary>
/// Standard provider names for <see cref="PermissionGrant"/>. Values are short single-
/// character codes to minimise row width — extensible by consumers (e.g. "G" for group).
/// </summary>
public static class PermissionProviderNames
{
    /// <summary>Grant targets a user. <c>ProviderKey</c> = user id string.</summary>
    public const string User = "U";

    /// <summary>Grant targets a role. <c>ProviderKey</c> = role id string.</summary>
    public const string Role = "R";
}
