namespace Nac.Core.Auth;

/// <summary>
/// Lightweight DTO for user identity info. Returned by <see cref="IIdentityService"/>.
/// </summary>
public sealed record UserInfo(
    Guid Id,
    string Email,
    string? DisplayName,
    string? TenantId,
    IReadOnlyList<string> Roles);
