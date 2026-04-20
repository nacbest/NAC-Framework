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

    /// <summary>
    /// Generates a short-lived impersonation JWT carrying the RFC 8693 <c>act</c> claim.
    /// Subject is the host user (so <c>sub</c>/audit trail points at the real human), while
    /// <c>act.sub</c> also records that host user as the actor — tenant-scoping comes from
    /// <paramref name="tenantId"/> + <paramref name="roleIds"/>. <c>is_host</c> is deliberately
    /// omitted to prevent <c>Host.AccessAllTenants</c> leak through impersonation tokens.
    /// </summary>
    public ImpersonationTokenResult GenerateImpersonationToken(
        Guid subjectUserId, string tenantId, string email, string? name,
        IReadOnlyList<Guid> roleIds, Guid actorUserId, string jti, TimeSpan ttl)
    {
        var expiresAt = DateTime.UtcNow.Add(ttl);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subjectUserId.ToString()),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, jti),
        };
        if (!string.IsNullOrEmpty(name))
            claims.Add(new(ClaimTypes.Name, name));
        claims.Add(new(NacIdentityClaims.TenantId, tenantId));
        if (roleIds is { Count: > 0 })
            claims.Add(new(NacIdentityClaims.RoleIds, JsonSerializer.Serialize(roleIds)));
        // is_host DELIBERATELY OMITTED — impersonator runs as tenant-scoped principal only.
        var actJson = JsonSerializer.Serialize(new { sub = actorUserId.ToString() });
        claims.Add(new Claim(NacIdentityClaims.ActClaim, actJson, JsonClaimValueTypes.Json));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);
        return new ImpersonationTokenResult(new JwtSecurityTokenHandler().WriteToken(token), jti, expiresAt);
    }
}
