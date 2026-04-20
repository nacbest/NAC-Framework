using FluentAssertions;
using Nac.Identity.Impersonation;
using Xunit;

namespace Nac.Identity.IntegrationTests.Impersonation;

/// <summary>
/// H3-I4, I11: Tenant pinning and revoke permission validation.
/// I4: Impersonation token carries a tenant_id claim, pinning it to that tenant.
///     Full middleware-level enforcement (rejecting header override when act present)
///     is deferred to v3.1.1 integration tests.
/// I11: RevokeAsync permission check — only owner or holder of Host.ImpersonateTenant can revoke.
/// </summary>
public class ImpersonationTenantPinAndRevokeTests
{
    /// <summary>
    /// I4 (validation): ImpersonationService.IssueAsync encodes the tenant_id claim into the JWT.
    /// This test verifies token structure includes the tenant_id claim.
    /// </summary>
    [Fact]
    public void IssueAsync_TokenStructureIncludesTenantIdClaim()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var tenantId = "tenant-pin-test";
        var jti = Guid.NewGuid().ToString("N");
        var secretKey = "this-is-a-very-long-secret-key-at-least-32-chars";

        var jwtService = new Nac.Identity.Jwt.JwtTokenService(
            Microsoft.Extensions.Options.Options.Create(new Nac.Identity.Jwt.JwtOptions
            {
                SecretKey = secretKey,
                Issuer = "test",
                Audience = "test",
                ExpirationMinutes = 60,
            }));

        // Act: generate impersonation token
        var token = jwtService.GenerateImpersonationToken(
            subjectUserId: hostUserId,
            tenantId: tenantId,
            email: "user@example.com",
            name: "Test User",
            roleIds: new Guid[] { },
            actorUserId: hostUserId,
            jti: jti,
            ttl: System.TimeSpan.FromMinutes(15));

        // Assert: decode and verify tenant_id claim
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token.Token);
        var tenantClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "tenant_id");

        tenantClaim.Should().NotBeNull(
            "Impersonation token must include tenant_id claim for pinning");
        tenantClaim!.Value.Should().Be(tenantId,
            "tenant_id claim must match the issued tenant");
    }

    /// <summary>
    /// I11 (service-level): Session.Revoke transitions RevokedAt to non-null.
    /// Permission enforcement is tested in ImpersonationServiceSecurityTests.
    /// This test validates the aggregate behavior after revocation.
    /// </summary>
    [Fact]
    public void RevokeAsync_MarkSessionAsRevoked()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var session = ImpersonationSession.Issue(
            hostUserId, "tenant-revoke-test", "I11 revoke test",
            Guid.NewGuid().ToString("N"), System.TimeSpan.FromMinutes(15));

        session.Should().NotBeNull();
        session.RevokedAt.Should().BeNull("session should not be revoked initially");

        // Act
        var revokeTime = DateTime.UtcNow;
        session.Revoke(revokeTime);

        // Assert
        session.RevokedAt.Should().Be(revokeTime,
            "session must be marked as revoked with correct timestamp");
    }
}
