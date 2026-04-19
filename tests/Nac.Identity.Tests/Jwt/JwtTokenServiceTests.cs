using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nac.Identity.Jwt;
using Nac.Identity.Services;
using Xunit;

namespace Nac.Identity.Tests.Jwt;

public class JwtTokenServiceTests
{
    private const string TestSecretKey = "this-is-a-very-long-secret-key-that-is-at-least-32-characters-long";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";
    private const int TestExpirationMinutes = 60;

    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        var options = Options.Create(new JwtOptions
        {
            SecretKey = TestSecretKey,
            Issuer = TestIssuer,
            Audience = TestAudience,
            ExpirationMinutes = TestExpirationMinutes,
        });
        _service = new JwtTokenService(options);
    }

    [Fact]
    public void GenerateToken_WithValidParams_ProducesValidJwt()
    {
        var token = _service.GenerateToken(Guid.NewGuid(), null, "u@example.com", null, [], false);

        token.Should().NotBeNullOrEmpty();
        new JwtSecurityTokenHandler().CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_ContainsNameIdentifierClaim()
    {
        var userId = Guid.NewGuid();
        var token = _service.GenerateToken(userId, null, "u@example.com", null, [], false);
        var claims = DecodeToken(token);

        claims.Should().ContainSingle(c =>
            c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
    }

    [Fact]
    public void GenerateToken_ContainsEmailClaim()
    {
        var token = _service.GenerateToken(Guid.NewGuid(), null, "test@example.com", null, [], false);
        var claims = DecodeToken(token);

        claims.Should().ContainSingle(c => c.Type == ClaimTypes.Email && c.Value == "test@example.com");
    }

    [Fact]
    public void GenerateToken_WithTenantId_ContainsTenantIdClaim()
    {
        var token = _service.GenerateToken(Guid.NewGuid(), "tenant-123", "u@example.com", null, [], false);
        var claims = DecodeToken(token);

        claims.Should().ContainSingle(c =>
            c.Type == NacIdentityClaims.TenantId && c.Value == "tenant-123");
    }

    [Fact]
    public void GenerateToken_WithoutTenantId_NoTenantIdClaim()
    {
        var token = _service.GenerateToken(Guid.NewGuid(), null, "u@example.com", null, [], false);
        var claims = DecodeToken(token);

        claims.Should().NotContain(c => c.Type == NacIdentityClaims.TenantId);
    }

    [Fact]
    public void GenerateToken_WithName_ContainsNameClaim()
    {
        var token = _service.GenerateToken(Guid.NewGuid(), null, "u@example.com", "John Doe", [], false);
        var claims = DecodeToken(token);

        claims.Should().ContainSingle(c => c.Type == ClaimTypes.Name && c.Value == "John Doe");
    }

    [Fact]
    public void GenerateToken_WithIsHost_ContainsIsHostClaim()
    {
        var token = _service.GenerateToken(Guid.NewGuid(), null, "host@platform.local", null, [], true);
        var claims = DecodeToken(token);

        claims.Should().ContainSingle(c =>
            c.Type == NacIdentityClaims.IsHost && c.Value == "true");
    }

    [Fact]
    public void GenerateToken_WithoutIsHost_NoIsHostClaim()
    {
        var token = _service.GenerateToken(Guid.NewGuid(), null, "u@example.com", null, [], false);
        var claims = DecodeToken(token);

        claims.Should().NotContain(c => c.Type == NacIdentityClaims.IsHost);
    }

    [Fact]
    public void GenerateToken_ContainsCorrectIssuer()
    {
        var token = _service.GenerateToken(Guid.NewGuid(), null, "u@example.com", null, [], false);
        var jwtToken = new JwtSecurityTokenHandler().ReadToken(token) as JwtSecurityToken;

        jwtToken!.Issuer.Should().Be(TestIssuer);
    }

    [Fact]
    public void GenerateToken_ContainsCorrectAudience()
    {
        var token = _service.GenerateToken(Guid.NewGuid(), null, "u@example.com", null, [], false);
        var jwtToken = new JwtSecurityTokenHandler().ReadToken(token) as JwtSecurityToken;

        jwtToken!.Audiences.Should().Contain(TestAudience);
    }

    [Fact]
    public void GenerateToken_ExpiresAtCorrectTime()
    {
        var before = DateTime.UtcNow;
        var token = _service.GenerateToken(Guid.NewGuid(), null, "u@example.com", null, [], false);
        var jwtToken = new JwtSecurityTokenHandler().ReadToken(token) as JwtSecurityToken;

        jwtToken!.ValidTo.Should().BeCloseTo(
            before.AddMinutes(TestExpirationMinutes), TimeSpan.FromSeconds(5));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<Claim> DecodeToken(string token)
    {
        var jwtToken = new JwtSecurityTokenHandler().ReadToken(token) as JwtSecurityToken;
        return jwtToken!.Claims.ToList();
    }
}
