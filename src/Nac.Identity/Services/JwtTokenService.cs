using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nac.Identity.Entities;
using Nac.Identity.Models;
using Nac.Identity.Options;

namespace Nac.Identity.Services;

/// <summary>
/// JWT token generation and validation service.
/// Generic over TUser to support custom user types extending <see cref="NacIdentityUser"/>.
/// </summary>
public class JwtTokenService<TUser> : IJwtTokenService<TUser>
    where TUser : NacIdentityUser
{
    private readonly NacIdentityOptions _options;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly UserManager<TUser> _userManager;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(
        IOptions<NacIdentityOptions> options,
        IRefreshTokenStore refreshTokenStore,
        UserManager<TUser> userManager)
    {
        _options = options.Value;
        _refreshTokenStore = refreshTokenStore;
        _userManager = userManager;

        if (string.IsNullOrEmpty(_options.SigningKey))
            throw new InvalidOperationException(
                "NacIdentity:SigningKey must be configured. Min 32 characters.");

        if (_options.SigningKey.Length < 32)
            throw new InvalidOperationException(
                "NacIdentity:SigningKey must be at least 32 characters.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public async Task<TokenResult> GenerateTokensAsync(
        TUser user,
        string? tenantId = null,
        string? deviceInfo = null)
    {
        var now = DateTimeOffset.UtcNow;
        var accessExpiry = now.Add(_options.AccessTokenExpiry);
        var refreshExpiry = now.Add(_options.RefreshTokenExpiry);

        // Generate access token
        var accessToken = GenerateAccessToken(user, tenantId, now, accessExpiry);

        // Generate refresh token (opaque, random)
        var refreshToken = GenerateRefreshToken();

        // Store refresh token (preserve tenantId for token refresh)
        await _refreshTokenStore.StoreAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = refreshExpiry,
            CreatedAt = now,
            DeviceInfo = deviceInfo,
            TenantId = tenantId
        });

        return new TokenResult(
            accessToken,
            refreshToken,
            accessExpiry,
            refreshExpiry);
    }

    public async Task<TokenResult?> RefreshTokensAsync(
        string refreshToken,
        string? deviceInfo = null)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await _refreshTokenStore.GetByHashAsync(tokenHash);

        if (storedToken is null || !storedToken.IsActive)
            return null;

        // Revoke old token (rotation)
        await _refreshTokenStore.RevokeAsync(tokenHash);

        // Load user for new token generation (User may be null for Redis store)
        var user = storedToken.User is TUser typedUser
            ? typedUser
            : await _userManager.FindByIdAsync(storedToken.UserId.ToString());

        if (user is null)
            return null;

        // Preserve tenant context from original token issuance
        return await GenerateTokensAsync(user, storedToken.TenantId, deviceInfo);
    }

    public async Task RevokeAllTokensAsync(Guid userId)
    {
        await _refreshTokenStore.RevokeAllForUserAsync(userId);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        await _refreshTokenStore.RevokeAsync(tokenHash);
    }

    private string GenerateAccessToken(
        TUser user,
        string? tenantId,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, issuedAt.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.DisplayName ?? user.UserName ?? string.Empty),
        };

        if (!string.IsNullOrEmpty(tenantId))
        {
            claims.Add(new Claim("tenant_id", tenantId));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    internal static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

/// <summary>
/// Non-generic convenience alias using <see cref="NacIdentityUser"/> directly.
/// </summary>
public sealed class JwtTokenService : JwtTokenService<NacIdentityUser>, IJwtTokenService
{
    public JwtTokenService(
        IOptions<NacIdentityOptions> options,
        IRefreshTokenStore refreshTokenStore,
        UserManager<NacIdentityUser> userManager)
        : base(options, refreshTokenStore, userManager)
    {
    }
}
