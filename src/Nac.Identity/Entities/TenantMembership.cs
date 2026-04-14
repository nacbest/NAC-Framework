namespace Nac.Identity.Entities;

/// <summary>
/// Links a user to a tenant with a specific role.
/// A user can belong to multiple tenants with different roles.
/// </summary>
public sealed class TenantMembership
{
    public Guid Id { get; set; }

    /// <summary>User ID (FK to NacUser).</summary>
    public Guid UserId { get; set; }

    /// <summary>User navigation property.</summary>
    public NacUser? User { get; set; }

    /// <summary>Tenant identifier.</summary>
    public required string TenantId { get; set; }

    /// <summary>Role assigned in this tenant (FK to TenantRole).</summary>
    public Guid TenantRoleId { get; set; }

    /// <summary>Role navigation property.</summary>
    public TenantRole? TenantRole { get; set; }

    /// <summary>Whether this user is the tenant owner (creator).</summary>
    public bool IsOwner { get; set; }

    /// <summary>When user joined this tenant.</summary>
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
