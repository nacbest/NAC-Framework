namespace Nac.Identity.Contracts.Auth;

/// <summary>
/// A single membership entry returned inside <see cref="LoginResponse"/>.
/// </summary>
public sealed record MembershipListItem(
    string TenantId,
    string? TenantName,
    IReadOnlyList<Guid> RoleIds,
    string Status,
    bool IsDefault);
