namespace Nac.Identity.Management.Contracts;

/// <summary>Lightweight user summary for list endpoints.</summary>
public sealed record UserSummaryDto(
    Guid Id,
    string? Email,
    string? FullName,
    bool IsActive,
    bool IsHost);

/// <summary>User detail including active memberships within the current tenant.</summary>
public sealed record UserDetailDto(
    Guid Id,
    string? Email,
    string? FullName,
    bool IsActive,
    bool IsHost,
    IReadOnlyList<MembershipDto> TenantMemberships);

/// <summary>Request to page/search users within the current tenant.</summary>
public sealed record UserListQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20);
