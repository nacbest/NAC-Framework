using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nac.Identity.Services;
using Nac.Identity.Users;

namespace Nac.Identity.Jwt;

/// <summary>
/// Generates signed JWT tokens for authenticated <see cref="NacUser"/> instances.
/// Includes userId, email, tenant_id, roles, and permission claims.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options, UserManager<NacUser> userManager)
{
    private readonly JwtOptions _options = options.Value;

    /// <summary>
    /// Generates a signed JWT token for the given user.
    /// Roles and permission claims are fetched from the identity store.
    /// </summary>
    /// <param name="user">The authenticated NAC user.</param>
    /// <returns>A signed JWT token string.</returns>
    public async Task<string> GenerateTokenAsync(NacUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var claims = await userManager.GetClaimsAsync(user);

        var tokenClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(NacIdentityClaims.TenantId, user.TenantId),
        };

        tokenClaims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        tokenClaims.AddRange(claims.Where(c => c.Type == NacIdentityClaims.Permission));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: tokenClaims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
