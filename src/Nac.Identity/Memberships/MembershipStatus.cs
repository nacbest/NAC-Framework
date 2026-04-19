namespace Nac.Identity.Memberships;

/// <summary>
/// Lifecycle status of a <see cref="UserTenantMembership"/>.
/// </summary>
public enum MembershipStatus
{
    /// <summary>User has been invited but not yet accepted.</summary>
    Invited = 0,

    /// <summary>Membership is active — user can sign in to the tenant.</summary>
    Active = 1,

    /// <summary>Membership is temporarily disabled (reversible).</summary>
    Suspended = 2,

    /// <summary>Membership was removed (terminal state; kept for audit).</summary>
    Removed = 3,
}
