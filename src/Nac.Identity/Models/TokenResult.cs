namespace Nac.Identity.Models;

/// <summary>
/// Result of token generation containing access and refresh tokens.
/// </summary>
public sealed record TokenResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt
);
