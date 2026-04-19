using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nac.Identity.Services;

namespace Nac.Identity.Jwt;

/// <summary>
/// Generates signed JWT tokens with the Pattern A minimal claim shape:
/// <c>sub, email, name?, tenant_id?, role_ids?, is_host?</c>. Permission claims are
/// never embedded — resolve at request time via <c>IPermissionChecker</c>. This service
/// is pure (no DB reads); callers pass already-resolved identity data.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    /// <summary>
    /// Generates a signed JWT with the minimal Pattern A claim set.
    /// </summary>
    /// <param name="userId">Subject (<c>sub</c>).</param>
    /// <param name="tenantId">Selected tenant slug; <c>null</c> = tenantless (e.g. login).</param>
    /// <param name="email">User email.</param>
    /// <param name="name">Optional display name.</param>
    /// <param name="roleIds">Role ids granted within <paramref name="tenantId"/> — empty if tenantless.</param>
    /// <param name="isHost">True if caller is a host account.</param>
    /// <returns>Signed JWT token string.</returns>
    public string GenerateToken(Guid userId, string? tenantId, string email, string? name,
                                IReadOnlyList<Guid> roleIds, bool isHost)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
        };
        if (!string.IsNullOrEmpty(name))
            claims.Add(new(ClaimTypes.Name, name));
        if (!string.IsNullOrEmpty(tenantId))
            claims.Add(new(NacIdentityClaims.TenantId, tenantId));
        if (roleIds is { Count: > 0 })
            claims.Add(new(NacIdentityClaims.RoleIds, JsonSerializer.Serialize(roleIds)));
        if (isHost)
            claims.Add(new(NacIdentityClaims.IsHost, "true"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
