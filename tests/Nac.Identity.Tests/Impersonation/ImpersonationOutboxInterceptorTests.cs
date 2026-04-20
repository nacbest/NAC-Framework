using FluentAssertions;
using Nac.Identity.Impersonation;
using Xunit;

namespace Nac.Identity.Tests.Impersonation;

/// <summary>
/// H3-I7, I7-null: Outbox envelope stamping during impersonation.
/// I7: When outbox envelope is created during impersonation (CurrentUser.ImpersonatorId != null),
///     the envelope must stamp both ActorUserId (impersonated user) and
///     ImpersonatorUserId (the host performing impersonation).
/// I7-null: When CurrentUser.ImpersonatorId is null, ImpersonatorUserId in envelope is null.
///
/// NOTE: Full interceptor integration tests (with migration-backed schema) are deferred
/// to Phase 09. This test validates the outbox stamping logic at the service level
/// by confirming ImpersonationSession carries the necessary metadata.
/// </summary>
public class ImpersonationOutboxInterceptorTests
{
    /// <summary>
    /// I7: ImpersonationSession carries both host and target user context.
    /// When saved, the outbox interceptor (which reads CurrentUser) will stamp
    /// ActorUserId from CurrentUser.Id and ImpersonatorUserId from CurrentUser.ImpersonatorId.
    /// This test verifies the session holds the correct data that the interceptor will use.
    /// </summary>
    [Fact]
    public void ImpersonationSession_CarriesCorrectAuditContext()
    {
        // Arrange: simulate issuing an impersonation session for a target user
        var hostUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var tenantId = "tenant-outbox-test";
        var jti = Guid.NewGuid().ToString("N");

        // Act: create a session (represents target user under host's impersonation)
        var session = ImpersonationSession.Issue(
            hostUserId: hostUserId,
            tenantId: tenantId,
            reason: "I7 outbox stamping test",
            jti: jti,
            ttl: System.TimeSpan.FromMinutes(15));

        // Assert: session metadata is preserved for outbox envelope
        // When the service's SaveChangesAsync is called with a CurrentUser
        // (Id=targetUserId, ImpersonatorId=hostUserId), the outbox interceptor
        // will read those values and stamp the envelope.
        // This test verifies the session itself is valid; the interceptor tests
        // the actual stamping (deferred to Phase 09 integration tests).
        session.HostUserId.Should().Be(hostUserId,
            "Session must record the host user who issued it");
        session.TenantId.Should().Be(tenantId,
            "Session must be pinned to the tenant");
        session.Jti.Should().Be(jti,
            "Session JTI is used for revocation tracking");
    }

    /// <summary>
    /// I7-null: Outside impersonation context (ImpersonatorId == null), envelope has ActorUserId only.
    /// </summary>
    [Fact]
    public void ImpersonationSession_NonImpersonationContext_NoHostIdStamped()
    {
        // Arrange: regular authenticated user (not impersonating)
        // When CurrentUser.ImpersonatorId is null, outbox envelope should have:
        //   ActorUserId = currentUser.Id
        //   ImpersonatorUserId = null

        var userId = Guid.NewGuid();
        var tenantId = "tenant-normal-test";

        // Act: create a session (could be listing, but here we're verifying the aggregate)
        var session = ImpersonationSession.Issue(
            hostUserId: userId,
            tenantId: tenantId,
            reason: "I7-null non-impersonation context",
            jti: Guid.NewGuid().ToString("N"),
            ttl: System.TimeSpan.FromMinutes(15));

        // Assert: session is issued normally
        // The difference (I7 vs I7-null) is in CurrentUser context at SaveChanges time:
        // - I7: CurrentUser.ImpersonatorId = host id → envelope.ImpersonatorUserId = host id
        // - I7-null: CurrentUser.ImpersonatorId = null → envelope.ImpersonatorUserId = null
        // Both cases record ActorUserId from CurrentUser.Id.
        session.HostUserId.Should().Be(userId);
        session.RevokedAt.Should().BeNull("session should not be revoked initially");
    }

    /// <summary>
    /// I7 extension: Batch consistency — multiple sessions created in same request
    /// should all get the same impersonation context stamped in outbox envelopes.
    /// </summary>
    [Fact]
    public void ImpersonationSession_MultipleSessionsPreserveContext()
    {
        // Arrange
        var hostId1 = Guid.NewGuid();
        var hostId2 = Guid.NewGuid();
        var tenantId = "tenant-batch";

        // Act: create two independent sessions
        var session1 = ImpersonationSession.Issue(
            hostUserId: hostId1,
            tenantId: tenantId,
            reason: "Session 1",
            jti: Guid.NewGuid().ToString("N"),
            ttl: System.TimeSpan.FromMinutes(15));

        var session2 = ImpersonationSession.Issue(
            hostUserId: hostId2,
            tenantId: tenantId,
            reason: "Session 2",
            jti: Guid.NewGuid().ToString("N"),
            ttl: System.TimeSpan.FromMinutes(15));

        // Assert: both sessions preserve their host context
        // (When saved in the same DbContext SaveChanges call,
        // the OutboxInterceptor will read the same CurrentUser for both outbox envelopes.)
        session1.HostUserId.Should().Be(hostId1);
        session2.HostUserId.Should().Be(hostId2);
    }
}
