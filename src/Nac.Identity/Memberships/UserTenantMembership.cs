using Nac.Core.Primitives;
using Nac.Identity.Users;

namespace Nac.Identity.Memberships;

/// <summary>
/// Join entity linking a global <see cref="NacUser"/> to a tenant. A user may have
/// multiple memberships (one per tenant) with independent role assignments per tenant.
/// </summary>
public sealed class UserTenantMembership : IAuditableEntity, ISoftDeletable
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Foreign key to <see cref="NacUser"/>.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Tenant slug (opaque reference; tenant directory owned by MT.Management).</summary>
    public string TenantId { get; private set; } = default!;

    /// <summary>Lifecycle status.</summary>
    public MembershipStatus Status { get; set; }

    /// <summary>UTC timestamp when the user joined (status transitioned to Active).</summary>
    public DateTime? JoinedAt { get; set; }

    /// <summary>Id of the user who sent the invite (null if self-signup or system).</summary>
    public Guid? InvitedBy { get; set; }

    /// <summary>When true this is the default tenant after login (one per user).</summary>
    public bool IsDefault { get; set; }

    /// <summary>Navigation — roles assigned within this membership.</summary>
    public ICollection<MembershipRole> Roles { get; private set; } = new List<MembershipRole>();

    // ── IAuditableEntity ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public string? CreatedBy { get; set; }

    // ── ISoftDeletable ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <inheritdoc/>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Required by EF Core.</summary>
    private UserTenantMembership() { }

    public UserTenantMembership(Guid userId, string tenantId, MembershipStatus status = MembershipStatus.Invited,
                                Guid? invitedBy = null, bool isDefault = false)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TenantId = tenantId;
        Status = status;
        InvitedBy = invitedBy;
        IsDefault = isDefault;
        if (status == MembershipStatus.Active) JoinedAt = DateTime.UtcNow;
    }

    /// <summary>Activates an invited or suspended membership.</summary>
    public void Activate()
    {
        Status = MembershipStatus.Active;
        JoinedAt ??= DateTime.UtcNow;
    }
}
