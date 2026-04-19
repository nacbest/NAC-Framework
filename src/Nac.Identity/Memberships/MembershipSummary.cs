namespace Nac.Identity.Memberships;

/// <summary>
/// Flat projection of a <see cref="UserTenantMembership"/> plus its role ids. Used by
/// APIs and the tenant-switch service to avoid exposing EF-tracked entities.
/// </summary>
public sealed record MembershipSummary(
    Guid Id,
    Guid UserId,
    string TenantId,
    MembershipStatus Status,
    IReadOnlyList<Guid> RoleIds,
    bool IsDefault,
    DateTime? JoinedAt);
