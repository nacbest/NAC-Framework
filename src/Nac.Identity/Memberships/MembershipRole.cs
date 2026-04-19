using Nac.Identity.Users;

namespace Nac.Identity.Memberships;

/// <summary>
/// Join entity assigning a tenant-scoped <see cref="NacRole"/> to a
/// <see cref="UserTenantMembership"/>. Composite key (MembershipId, RoleId).
/// </summary>
public sealed class MembershipRole
{
    /// <summary>Foreign key to <see cref="UserTenantMembership"/>.</summary>
    public Guid MembershipId { get; private set; }

    /// <summary>Foreign key to <see cref="NacRole"/>.</summary>
    public Guid RoleId { get; private set; }

    /// <summary>UTC timestamp when the role was assigned.</summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>Required by EF Core.</summary>
    private MembershipRole() { }

    public MembershipRole(Guid membershipId, Guid roleId)
    {
        MembershipId = membershipId;
        RoleId = roleId;
        AssignedAt = DateTime.UtcNow;
    }
}
