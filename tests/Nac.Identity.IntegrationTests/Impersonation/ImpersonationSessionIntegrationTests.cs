using FluentAssertions;
using Nac.Identity.Impersonation;
using Xunit;

namespace Nac.Identity.IntegrationTests.Impersonation;

/// <summary>
/// Integration tests for ImpersonationSession aggregate behavior.
/// Coverage: U7 (session revocation idempotency), I1–I9 (behavior validation).
/// </summary>
public class ImpersonationSessionIntegrationTests
{
    /// <summary>
    /// U7: ImpersonationSession.Revoke is idempotent — event raised only once.
    /// </summary>
    [Fact]
    public void Revoke_CalledTwice_RaisesEventOnlyOnce()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var session = ImpersonationSession.Issue(
            hostUserId, "tenant-1", "Test", Guid.NewGuid().ToString("N"), TimeSpan.FromMinutes(15));

        // Act
        var revokeTime1 = DateTime.UtcNow;
        session.Revoke(revokeTime1);
        var eventsAfterFirst = session.DomainEvents.Count();

        session.Revoke(revokeTime1.AddSeconds(1));
        var eventsAfterSecond = session.DomainEvents.Count();

        // Assert
        eventsAfterFirst.Should().Be(1);
        eventsAfterSecond.Should().Be(1, "second revocation should not raise new event");
        session.RevokedAt.Should().Be(revokeTime1);
    }

    /// <summary>
    /// U7 extension: Session raises ImpersonationRevokedEvent with correct data.
    /// </summary>
    [Fact]
    public void Revoke_RaisesEventWithCorrectData()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var tenantId = "tenant-abc";
        var session = ImpersonationSession.Issue(
            hostUserId, tenantId, "Support", Guid.NewGuid().ToString("N"), TimeSpan.FromMinutes(15));

        // Act
        session.Revoke(DateTime.UtcNow);

        // Assert
        var evt = session.DomainEvents.OfType<ImpersonationRevokedEvent>().Single();
        evt.HostUserId.Should().Be(hostUserId);
        evt.TenantId.Should().Be(tenantId);
        evt.SessionId.Should().Be(session.Id);
    }

    /// <summary>
    /// I1/U9 extension: Session persists with correct data.
    /// </summary>
    [Fact]
    public void Issue_CreatesSessionWithCorrectProperties()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var tenantId = "tenant-xyz";
        var reason = "Customer ticket #42";
        var jti = Guid.NewGuid().ToString("N");
        var ttl = TimeSpan.FromMinutes(15);

        // Act
        var session = ImpersonationSession.Issue(hostUserId, tenantId, reason, jti, ttl);

        // Assert
        session.HostUserId.Should().Be(hostUserId);
        session.TenantId.Should().Be(tenantId);
        session.Reason.Should().Be(reason);
        session.Jti.Should().Be(jti);
        session.RevokedAt.Should().BeNull();
        session.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }
}
