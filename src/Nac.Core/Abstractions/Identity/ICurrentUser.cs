namespace Nac.Core.Abstractions.Identity;

/// <summary>
/// Read-only projection of the authenticated user for the current request.
/// Pattern A: users are global — <see cref="TenantId"/> is the currently selected tenant
/// (from JWT), not an immutable user attribute.
/// </summary>
public interface ICurrentUser
{
    /// <summary>User id (<c>sub</c> claim).</summary>
    Guid Id { get; }

    /// <summary>User email (may be absent in some auth flows).</summary>
    string? Email { get; }

    /// <summary>Display name.</summary>
    string? Name { get; }

    /// <summary>Currently selected tenant slug from JWT. Null on tenantless sessions.</summary>
    string? TenantId { get; }

    /// <summary>Role ids granted within the current tenant.</summary>
    IReadOnlyList<Guid> RoleIds { get; }

    /// <summary>True when the authentication ticket is valid.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Host (platform) account flag — see <c>Host.AccessAllTenants</c>.</summary>
    bool IsHost { get; }
}
