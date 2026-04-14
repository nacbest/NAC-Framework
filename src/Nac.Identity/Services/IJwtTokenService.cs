using Nac.Identity.Entities;
using Nac.Identity.Models;

namespace Nac.Identity.Services;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates access and refresh tokens for a user.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="tenantId">Current tenant ID (null for non-tenant context).</param>
    /// <param name="deviceInfo">Optional device/client info for refresh token tracking.</param>
    /// <returns>Token result with access and refresh tokens.</returns>
    Task<TokenResult> GenerateTokensAsync(
        NacUser user,
        string? tenantId = null,
        string? deviceInfo = null);

    /// <summary>
    /// Validates a refresh token and generates new tokens.
    /// Implements token rotation (old refresh token invalidated).
    /// </summary>
    /// <param name="refreshToken">The refresh token to validate.</param>
    /// <param name="deviceInfo">Device info for the new refresh token.</param>
    /// <returns>New token result, or null if refresh token invalid.</returns>
    Task<TokenResult?> RefreshTokensAsync(
        string refreshToken,
        string? deviceInfo = null);

    /// <summary>
    /// Revokes all refresh tokens for a user.
    /// Use on password change, logout-all, or security concern.
    /// </summary>
    Task RevokeAllTokensAsync(Guid userId);

    /// <summary>
    /// Revokes a specific refresh token.
    /// </summary>
    Task RevokeTokenAsync(string refreshToken);
}
