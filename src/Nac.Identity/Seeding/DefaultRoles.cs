namespace Nac.Identity.Seeding;

/// <summary>
/// Default role definitions for new tenants.
/// </summary>
public static class DefaultRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Member = "Member";

    /// <summary>
    /// Gets the default role definitions.
    /// Override via NacIdentityOptions.DefaultRoles configuration.
    /// </summary>
    public static IReadOnlyList<DefaultRoleDefinition> GetDefaults() =>
    [
        new DefaultRoleDefinition
        {
            Name = Owner,
            Permissions = ["*"]
        },
        new DefaultRoleDefinition
        {
            Name = Admin,
            Permissions =
            [
                "catalog.*",
                "orders.*",
                "inventory.*",
                "users.read",
                "users.invite"
            ]
        },
        new DefaultRoleDefinition
        {
            Name = Member,
            Permissions =
            [
                "catalog.read",
                "orders.read",
                "orders.create",
                "inventory.read"
            ]
        }
    ];
}

/// <summary>
/// Role definition for seeding.
/// </summary>
public sealed class DefaultRoleDefinition
{
    public required string Name { get; init; }
    public List<string> Permissions { get; init; } = [];
}
