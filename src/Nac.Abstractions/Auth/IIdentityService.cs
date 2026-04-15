namespace Nac.Abstractions.Auth;

/// <summary>
/// Provides user identity lookups for business modules.
/// Implemented by Nac.Identity; consumed via DI in module handlers.
/// </summary>
public interface IIdentityService
{
    /// <summary>Gets user info by ID. Returns null if user not found.</summary>
    Task<UserInfo?> GetUserInfoAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Gets user info for multiple users in a batch.</summary>
    Task<IReadOnlyList<UserInfo>> GetUsersAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);

    /// <summary>Checks whether the user has been assigned a specific role.</summary>
    Task<bool> IsInRoleAsync(Guid userId, string role, CancellationToken ct = default);
}

/// <summary>
/// Lightweight DTO for user identity info. Returned by <see cref="IIdentityService"/>.
/// Does not carry tenant — consumer gets tenant from <see cref="Nac.Abstractions.MultiTenancy.ITenantContext"/>.
/// </summary>
public sealed record UserInfo(
    Guid Id,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);
