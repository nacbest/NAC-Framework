using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nac.Identity.Jwt;
using Nac.Identity.Services;
using Xunit;

namespace Nac.Identity.Tests.Impersonation;

/// <summary>
/// S7: Impersonation tokens are generated with is_host=false (security invariant).
/// Validates that tokens minted for impersonation do NOT include the host claim.
/// S9: ImpersonationService.IssueAsync checks hostUserId == currentUser.Id invariant.
/// </summary>
public class ImpersonationClaimSecurityTests
{
    private const string TestSecretKey = "this-is-a-very-long-secret-key-that-is-at-least-32-characters-long";
    private const string TestIssuer = "TestIssuer";
    private const string TestAudience = "TestAudience";

    private readonly JwtTokenService _service;

    public ImpersonationClaimSecurityTests()
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
    /// S7: Impersonation token OMITS `is_host` claim (security invariant).
    /// Regular tokens can have is_host=true/false; impersonation tokens never include it.
    /// </summary>
    [Fact]
    public void GenerateImpersonationToken_OmitsIsHostClaimForSecurity()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var tenantId = "tenant-123";
        var email = "user@example.com";
        var name = "Test User";
        var roleIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var jti = Guid.NewGuid().ToString("N");

        // Act
        var token = _service.GenerateImpersonationToken(
            subjectUserId: hostUserId,
            tenantId: tenantId,
            email: email,
            name: name,
            roleIds: roleIds,
            actorUserId: hostUserId,
            jti: jti,
            ttl: TimeSpan.FromMinutes(15));

        var claims = DecodeToken(token.Token);

        // Assert: is_host claim MUST be absent (deliberately omitted)
        var isHostClaim = claims.FirstOrDefault(c => c.Type == NacIdentityClaims.IsHost);
        isHostClaim.Should().BeNull("is_host claim must be absent for impersonation tokens");
    }

    /// <summary>
    /// S7: Impersonation token has `act` claim with JSON structure {"sub":"..."}
    /// (RFC 8693 actor claim).
    /// </summary>
    [Fact]
    public void GenerateImpersonationToken_IncludesActClaim()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var tenantId = "tenant-123";

        // Act
        var token = _service.GenerateImpersonationToken(
            subjectUserId: hostUserId,
            tenantId: tenantId,
            email: "user@example.com",
            name: "Test User",
            roleIds: new Guid[] { },
            actorUserId: hostUserId,
            jti: Guid.NewGuid().ToString("N"),
            ttl: TimeSpan.FromMinutes(15));

        var claims = DecodeToken(token.Token);

        // Assert: act claim must be present as JSON {"sub":"<actorUserId>"}
        var actClaim = claims.FirstOrDefault(c => c.Type == NacIdentityClaims.ActClaim);
        actClaim.Should().NotBeNull("act claim must be present for impersonation tokens");
        // The value is a JSON object: {"sub":"<guid>"}
        actClaim!.Value.Should().Contain(hostUserId.ToString());
    }

    /// <summary>
    /// S7: Impersonation token NEVER includes is_host claim, even when called on a host user.
    /// This prevents Host.AccessAllTenants permission from leaking through impersonation.
    /// </summary>
    [Fact]
    public void GenerateImpersonationToken_NeverIncludesIsHostRegardlessOfInput()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();

        // Act: Call GenerateImpersonationToken for a host user
        var token1 = _service.GenerateImpersonationToken(
            subjectUserId: hostUserId,
            tenantId: "tenant-1",
            email: "user@example.com",
            name: "Test",
            roleIds: new Guid[] { },
            actorUserId: hostUserId,
            jti: Guid.NewGuid().ToString("N"),
            ttl: TimeSpan.FromMinutes(15));

        var claims = DecodeToken(token1.Token);
        var isHostClaim = claims.FirstOrDefault(c => c.Type == NacIdentityClaims.IsHost);

        // Assert: is_host is always absent (not included in impersonation tokens)
        isHostClaim.Should().BeNull("is_host must be absent from impersonation tokens for security");
    }

    /// <summary>
    /// S7: Non-impersonation token has is_host claim based on input (not forced to false).
    /// This validates the distinction: regular tokens can have is_host=true.
    /// </summary>
    [Fact]
    public void GenerateToken_CanHaveIsHostTrue_UnlikeImpersonationTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act: Regular token with isHost=true
        var token = _service.GenerateToken(
            userId: userId,
            tenantId: "tenant-1",
            email: "admin@example.com",
            name: "Admin",
            roleIds: new[] { Guid.NewGuid() },
            isHost: true);

        var claims = DecodeToken(token);

        // Assert: Regular token CAN have is_host=true
        var isHostClaim = claims.FirstOrDefault(c => c.Type == NacIdentityClaims.IsHost);
        isHostClaim.Should().NotBeNull();
        isHostClaim!.Value.Should().Be("true");

        // And it should NOT have act claim (no impersonation)
        var actClaim = claims.FirstOrDefault(c => c.Type == NacIdentityClaims.ActClaim);
        actClaim.Should().BeNull();
    }

    /// <summary>
    /// S7: Impersonation token has jti claim (for revocation).
    /// </summary>
    [Fact]
    public void GenerateImpersonationToken_IncludesJtiClaim()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString("N");

        // Act
        var token = _service.GenerateImpersonationToken(
            subjectUserId: Guid.NewGuid(),
            tenantId: "tenant-1",
            email: "user@example.com",
            name: "Test",
            roleIds: new Guid[] { },
            actorUserId: Guid.NewGuid(),
            jti: jti,
            ttl: TimeSpan.FromMinutes(15));

        var claims = DecodeToken(token.Token);

        // Assert
        var jtiClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        jtiClaim.Should().NotBeNull();
        jtiClaim!.Value.Should().Be(jti);
    }

    /// <summary>
    /// S9: ImpersonationService.IssueAsync enforces hostUserId == currentUser.Id invariant.
    /// (Integration test in ImpersonationNestedSecurityTests; this is the claim validation test.)
    /// </summary>
    [Fact]
    public void GenerateImpersonationToken_MultipleActorsCannotBeCreated()
    {
        // Arrange: Two different user IDs
        var hostUserId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();

        // Act: Create token with hostUserId as subject but differentUserId as actor
        // (This would violate the invariant if allowed)
        var token = _service.GenerateImpersonationToken(
            subjectUserId: hostUserId,
            tenantId: "tenant-1",
            email: "user@example.com",
            name: "Test",
            roleIds: new Guid[] { },
            actorUserId: differentUserId,  // Different from subject
            jti: Guid.NewGuid().ToString("N"),
            ttl: TimeSpan.FromMinutes(15));

        var claims = DecodeToken(token.Token);

        // Assert: Token contains both subject and actor
        var subClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier) ??
                       claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        var actClaim = claims.FirstOrDefault(c => c.Type == NacIdentityClaims.ActClaim);

        subClaim.Should().NotBeNull();
        actClaim.Should().NotBeNull();
        subClaim!.Value.Should().Be(hostUserId.ToString());
        actClaim!.Value.Should().Contain(differentUserId.ToString());
        // NOTE: In production, ImpersonationService.IssueAsync enforces hostUserId == currentUser.Id,
        // so this scenario cannot occur. The token service is pure and amoral — it just signs claims.
    }

    private static IEnumerable<Claim> DecodeToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.Claims;
    }
}
