namespace Nac.Identity.Memberships;

/// <summary>
/// Manages <see cref="UserTenantMembership"/> lifecycle and role assignments.
/// Mutations that change role assignments invalidate the affected user's permission
/// cache key.
/// </summary>
public interface IMembershipService
{
    /// <summary>Invites a user to a tenant. Status = Invited; returns an opaque accept token.</summary>
    Task<(Guid membershipId, string inviteToken)> InviteAsync(Guid userId, string tenantId,
                                                              Guid invitedBy, IReadOnlyList<Guid> roleIds,
                                                              CancellationToken ct = default);

    /// <summary>Accepts an invite, flipping the membership to Active.</summary>
    Task AcceptAsync(string inviteToken, Guid userId, CancellationToken ct = default);

    /// <summary>Creates an already-Active membership (onboarding path; no invite token).</summary>
    Task<Guid> CreateActiveMembershipAsync(Guid userId, string tenantId, IReadOnlyList<Guid> roleIds,
                                           bool isDefault, CancellationToken ct = default);

    /// <summary>Lists all memberships for a user (across tenants).</summary>
    Task<IReadOnlyList<MembershipSummary>> ListForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Lists members of a tenant.</summary>
    Task<IReadOnlyList<MembershipSummary>> ListMembersAsync(string tenantId, CancellationToken ct = default);

    /// <summary>Replaces the set of roles on a membership; invalidates user permission cache.</summary>
    Task ChangeRolesAsync(Guid membershipId, IReadOnlyList<Guid> roleIds, CancellationToken ct = default);

    /// <summary>Removes a member (soft-delete; Status = Removed).</summary>
    Task RemoveMemberAsync(Guid membershipId, CancellationToken ct = default);

    /// <summary>Sets a specific membership as the user's default tenant.</summary>
    Task SetDefaultAsync(Guid userId, string tenantId, CancellationToken ct = default);

    /// <summary>Resolves role ids for a user within a tenant (used by JWT + checker).</summary>
    Task<IReadOnlyList<Guid>> GetRoleIdsAsync(Guid userId, string tenantId, CancellationToken ct = default);
}
