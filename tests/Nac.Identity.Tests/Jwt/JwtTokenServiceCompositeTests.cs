using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nac.Identity.Jwt;
using Nac.Identity.Services;
using Xunit;

namespace Nac.Identity.Tests.Jwt;

/// <summary>
/// Composite and integration tests for JwtTokenService Pattern A claim shape:
/// sub, email, name?, tenant_id?, role_ids?, is_host?
/// </summary>
public class JwtTokenServiceCompositeTests
{
    private const string TestSecretKey = "this-is-a-very-long-secret-key-that-is-at-least-32-characters-long";

    private readonly JwtTokenService _service = new(Options.Create(new JwtOptions
    {
        SecretKey = TestSecretKey,
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        ExpirationMinutes = 60,
    }));

    [Fact]
    public void GenerateToken_WithRoleIds_ContainsRoleIdsClaim()
    {
        var roleIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var token = _service.GenerateToken(Guid.NewGuid(), "t1", "u@x.com", null, roleIds, false);
        var claims = DecodeToken(token);

        var raw = claims.Single(c => c.Type == NacIdentityClaims.RoleIds).Value;
        var decoded = JsonSerializer.Deserialize<Guid[]>(raw);
        decoded.Should().BeEquivalentTo(roleIds);
    }

    [Fact]
    public void GenerateToken_WithEmptyRoleIds_NoRoleIdsClaim()
    {
        var token = _service.GenerateToken(Guid.NewGuid(), "t1", "u@x.com", null, [], false);
        var claims = DecodeToken(token);

        claims.Should().NotContain(c => c.Type == NacIdentityClaims.RoleIds);
    }

    [Fact]
    public void GenerateToken_AllRequiredClaimsPresent()
    {
        var userId = Guid.NewGuid();
        var roleIds = new[] { Guid.NewGuid() };
        var token = _service.GenerateToken(userId, "tenant-x", "u@x.com", "Full Name", roleIds, true);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        jwtToken!.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");
        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow);

        var claims = jwtToken.Claims.ToList();
        claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "u@x.com");
        claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "Full Name");
        claims.Should().Contain(c => c.Type == NacIdentityClaims.TenantId && c.Value == "tenant-x");
        claims.Should().Contain(c => c.Type == NacIdentityClaims.IsHost && c.Value == "true");
        claims.Should().Contain(c => c.Type == NacIdentityClaims.RoleIds);
    }

    [Fact]
    public void GenerateToken_NoPermissionClaims_EmbeddedInToken()
    {
        // Pattern A: permissions are NOT embedded in the JWT — resolved at request time
        var token = _service.GenerateToken(Guid.NewGuid(), "t1", "u@x.com", null, [], false);
        var claims = DecodeToken(token);

        claims.Should().NotContain(c => c.Type == NacIdentityClaims.Permission);
        claims.Should().NotContain(c => c.Type == ClaimTypes.Role);
    }

    [Fact]
    public void GenerateToken_TenantlessToken_NoTenantOrRoleIdsClaims()
    {
        var token = _service.GenerateToken(Guid.NewGuid(), null, "u@x.com", null, [], false);
        var claims = DecodeToken(token);

        claims.Should().NotContain(c => c.Type == NacIdentityClaims.TenantId);
        claims.Should().NotContain(c => c.Type == NacIdentityClaims.RoleIds);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<Claim> DecodeToken(string token)
    {
        var jwtToken = new JwtSecurityTokenHandler().ReadToken(token) as JwtSecurityToken;
        return jwtToken!.Claims.ToList();
    }
}
