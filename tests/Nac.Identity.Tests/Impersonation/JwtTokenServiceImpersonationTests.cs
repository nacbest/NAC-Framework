using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nac.Identity.Jwt;
using Nac.Identity.Services;
using Xunit;

namespace Nac.Identity.Tests.Impersonation;

/// <summary>
/// Unit tests for ImpersonationToken — verifies `act` claim, token TTL, and isHost=false invariant.
/// Coverage: U1–U2 matrix.
/// </summary>
public class JwtTokenServiceImpersonationTests
{
    private const string TestSecretKey = "this-is-a-very-long-secret-key-that-is-at-least-32-characters-long";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly JwtTokenService _service;

    public JwtTokenServiceImpersonationTests()
    {
        var options = Options.Create(new JwtOptions
        {
            SecretKey = TestSecretKey,
            Issuer = TestIssuer,
            Audience = TestAudience,
            ExpirationMinutes = 60,
        });
        _service = new JwtTokenService(options);
    }

    /// <summary>
    /// U1: GenerateToken (non-impersonation) has no `act` claim.
    /// </summary>
    [Fact]
    public void GenerateToken_WithoutActorUserId_HasNoActClaim()
    {
        var userId = Guid.NewGuid();
        var token = _service.GenerateToken(userId, "tenant-123", "user@example.com", null, [], false);
        var claims = DecodeToken(token);

        claims.Should().NotContain(c => c.Type == NacIdentityClaims.ActClaim);
    }

    /// <summary>
    /// U2: GenerateImpersonationToken has `act.sub` claim with actorUserId.
    /// </summary>
    [Fact]
    public void GenerateImpersonationToken_HasActClaimAndJti()
    {
        var hostUserId = Guid.NewGuid();
        var tenantId = "tenant-123";
        var jti = Guid.NewGuid().ToString("N");

        var result = _service.GenerateImpersonationToken(
            subjectUserId: Guid.NewGuid(),
            tenantId: tenantId,
            email: "host@example.com",
            name: "Host",
            roleIds: [],
            actorUserId: hostUserId,
            jti: jti,
            ttl: TimeSpan.FromMinutes(15));

        var claims = DecodeToken(result.Token);

        // Act claim must be present and contain actor id
        var actClaim = claims.FirstOrDefault(c => c.Type == NacIdentityClaims.ActClaim);
        actClaim.Should().NotBeNull();
        actClaim!.Value.Should().Contain(hostUserId.ToString());

        // Jti must be present
        var jtiClaim = claims.FirstOrDefault(c => c.Type == "jti");
        jtiClaim.Should().NotBeNull();
        jtiClaim!.Value.Should().Be(jti);

        // is_host must NOT be present (security invariant)
        var isHostClaim = claims.FirstOrDefault(c => c.Type == NacIdentityClaims.IsHost);
        isHostClaim.Should().BeNull();
    }

    /// <summary>
    /// U2 extension: Verify token TTL is applied correctly (approximately 15 minutes).
    /// </summary>
    [Fact]
    public void GenerateImpersonationToken_RespectsTtl()
    {
        var ttl = TimeSpan.FromMinutes(15);
        var result = _service.GenerateImpersonationToken(
            subjectUserId: Guid.NewGuid(),
            tenantId: "t1",
            email: "h@ex.com",
            name: "H",
            roleIds: [],
            actorUserId: Guid.NewGuid(),
            jti: Guid.NewGuid().ToString("N"),
            ttl: ttl);

        var handler = new JwtSecurityTokenHandler();
        var jwt = (handler.ReadToken(result.Token) as JwtSecurityToken)!;

        // Token expiry should be approximately 15 minutes from now
        var expectedExpiry = DateTime.UtcNow.Add(ttl);
        var actualTtl = jwt.ValidTo - DateTime.UtcNow;

        // Allow ±2 second tolerance for test execution time
        actualTtl.Should().BeCloseTo(ttl, TimeSpan.FromSeconds(2));
    }

    private static IReadOnlyList<Claim> DecodeToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadToken(token) as JwtSecurityToken
                  ?? throw new InvalidOperationException("Invalid JWT");
        return jwt.Claims.ToList();
    }
}
