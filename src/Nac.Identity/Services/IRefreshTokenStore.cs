using Nac.Identity.Entities;

namespace Nac.Identity.Services;

/// <summary>
/// Abstraction for refresh token persistence.
/// Implementations: EF Core (default), Redis (optional).
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>
    /// Stores a new refresh token.
    /// </summary>
    Task StoreAsync(RefreshToken token);

    /// <summary>
    /// Retrieves a token by its hash.
    /// Returns null if not found or expired.
    /// </summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash);

    /// <summary>
    /// Revokes a token by its hash (sets RevokedAt).
    /// </summary>
    Task RevokeAsync(string tokenHash);

    /// <summary>
    /// Revokes all tokens for a specific user.
    /// Used on password change or security concern.
    /// </summary>
    Task RevokeAllForUserAsync(Guid userId);

    /// <summary>
    /// Removes expired tokens from storage.
    /// Called periodically by background service.
    /// </summary>
    Task<int> CleanupExpiredAsync();
}
