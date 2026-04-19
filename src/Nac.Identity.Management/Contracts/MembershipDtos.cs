namespace Nac.Identity.Management.Contracts;

/// <summary>Flat membership projection returned by list/detail endpoints.</summary>
public sealed record MembershipDto(
    Guid Id,
    Guid UserId,
    string? Email,
    string TenantId,
    string Status,
    IReadOnlyList<Guid> RoleIds,
    DateTime? JoinedAt,
    bool IsDefault);

/// <summary>Invite a user (new or existing) to the current tenant.</summary>
public sealed record InviteRequest(string Email, IReadOnlyList<Guid> RoleIds);

/// <summary>Response after a successful invite.</summary>
public sealed record InviteResponse(Guid MembershipId, string InviteToken);

/// <summary>Replace role assignments on an existing membership.</summary>
public sealed record ChangeMembershipRolesRequest(IReadOnlyList<Guid> RoleIds);
