namespace Nac.Identity.Entities;

/// <summary>
/// JWT refresh token for token rotation.
/// Stored hashed; original token never persisted.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }

    /// <summary>User this token belongs to.</summary>
    public Guid UserId { get; set; }

    /// <summary>User navigation property.</summary>
    public NacUser? User { get; set; }

    /// <summary>SHA256 hash of the token value.</summary>
    public required string TokenHash { get; set; }

    /// <summary>Token expiration time.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>When token was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When token was revoked (null if active).</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Device/client info for audit.</summary>
    public string? DeviceInfo { get; set; }

    /// <summary>Whether token is still valid.</summary>
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTimeOffset.UtcNow;
}
