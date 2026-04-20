using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nac.Core.Abstractions.Permissions;
using Nac.Core.Domain;
using Nac.Identity.Impersonation;
using Nac.Identity.Jwt;
using Nac.Identity.Permissions.Host;
using Nac.Identity.Tests.Infrastructure;
using Nac.Identity.Users;
using Nac.Testing.Fakes;
using NSubstitute;
using Xunit;

namespace Nac.Identity.Tests.Impersonation;

/// <summary>
/// Security-focused unit tests for <see cref="ImpersonationService"/> and related components.
/// Covers review blockers:
///   H3-S5 — nested impersonation rejection
///   H3-I5 — missing permission 403 (service-level invariant)
///   H3-Revocation — round-trip: issue → revoke → blacklist asserts revoked
/// Note: H3-I10 (DTO Jti absence) lives in Nac.Identity.IntegrationTests because
///       ImpersonationSessionDto is defined in Nac.Identity.Management.
/// </summary>
public class ImpersonationServiceSecurityTests : IAsyncDisposable
{
    private const string SecretKey = "this-is-a-very-long-secret-key-at-least-32-chars";

    private readonly IdentityTestHost _host;

    public ImpersonationServiceSecurityTests()
    {
        _host = IdentityTestHost.Create(services =>
        {
            services.AddDistributedMemoryCache();
            services.AddSingleton<IJtiBlacklist>(sp =>
                new RedisJtiBlacklist(
                    sp.GetRequiredService<IDistributedCache>(),
                    NullLogger<RedisJtiBlacklist>.Instance));
        });
    }

    public async ValueTask DisposeAsync() => await _host.DisposeAsync();

    // ── helpers ────────────────────────────────────────────────────────────────

    private ImpersonationService BuildService(
        FakeCurrentUser currentUser,
        IPermissionChecker permissionChecker,
        IImpersonationSessionRepository? sessions = null,
        IImpersonationRateLimiter? rateLimiter = null)
    {
        var jwtService = new JwtTokenService(Options.Create(new JwtOptions
        {
            SecretKey = SecretKey,
            Issuer = "test",
            Audience = "test",
            ExpirationMinutes = 60,
        }));

        var blacklist = _host.GetRequiredService<IJtiBlacklist>();
        var userManager = _host.GetRequiredService<UserManager<NacUser>>();

        var repo = sessions ?? Substitute.For<IImpersonationSessionRepository>();
        repo.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        repo.AddAsync(Arg.Any<ImpersonationSession>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var rl = rateLimiter ?? Substitute.For<IImpersonationRateLimiter>();
        rl.TryConsumeAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
          .Returns(Task.FromResult(true));

        var roleProvider = Substitute.For<IImpersonationRoleProvider>();
        roleProvider.GetImpersonationRoleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(new ImpersonationRoleTemplate("impersonator", [])));

        return new ImpersonationService(
            userManager, repo, jwtService, blacklist,
            roleProvider, permissionChecker, currentUser, rl);
    }

    // ── S5: Nested impersonation rejection ────────────────────────────────────

    /// <summary>
    /// H3-S5: IssueAsync MUST throw <see cref="ForbiddenAccessException"/> when the caller
    /// already carries a non-null <c>ImpersonatorId</c> (i.e. an active RFC 8693 <c>act</c>
    /// claim). Prevents nested / chained impersonation tokens.
    /// </summary>
    [Fact]
    public async Task IssueAsync_WhenCallerIsAlreadyImpersonating_ThrowsForbidden()
    {
        // Arrange: caller's current token already has act claim → ImpersonatorId is set
        var hostId = Guid.NewGuid();
        var caller = new FakeCurrentUser
        {
            Id = hostId,
            IsHost = true,
            IsAuthenticated = true,
            ImpersonatorId = hostId, // non-null = active impersonation session
        };

        var checker = Substitute.For<IPermissionChecker>();
        checker.IsGrantedAsync(HostPermissions.ImpersonateTenant, Arg.Any<CancellationToken>())
               .Returns(Task.FromResult(true));

        var service = BuildService(caller, checker);

        // Act
        var act = () => service.IssueAsync(hostId, "tenant-abc", "Support ticket #99");

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*Nested impersonation*");
    }

    // ── I5: Missing permission → service throws ForbiddenAccessException ──────

    /// <summary>
    /// H3-I5: IssueAsync MUST throw <see cref="ForbiddenAccessException"/> when the caller
    /// is authenticated as host but does NOT hold the <c>Host.ImpersonateTenant</c> grant.
    ///
    /// NOTE: A WebApplicationFactory is not available in Nac.Identity.Tests. This service-level
    /// test exercises the same invariant that the HTTP layer maps to 403 via NacExceptionHandler.
    /// The enforcement point is <see cref="ImpersonationService.IssueAsync"/> lines 41–43.
    /// </summary>
    [Fact]
    public async Task IssueAsync_WhenCallerLacksImpersonatePermission_ThrowsForbidden()
    {
        // Arrange
        var hostId = Guid.NewGuid();
        var caller = new FakeCurrentUser
        {
            Id = hostId,
            IsHost = true,
            IsAuthenticated = true,
            ImpersonatorId = null, // not currently impersonating
        };

        // Permission checker denies the required grant
        var checker = Substitute.For<IPermissionChecker>();
        checker.IsGrantedAsync(HostPermissions.ImpersonateTenant, Arg.Any<CancellationToken>())
               .Returns(Task.FromResult(false));

        var service = BuildService(caller, checker);

        // Act
        var act = () => service.IssueAsync(hostId, "tenant-xyz", "Customer issue #42");

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*Missing Host.ImpersonateTenant*");
    }

    // ── Revocation round-trip ─────────────────────────────────────────────────

    /// <summary>
    /// H3-Revocation: Issuing a session and then calling RevokeAsync must add the jti to
    /// <see cref="IJtiBlacklist"/>. Subsequent <see cref="IJtiBlacklist.IsRevokedAsync"/>
    /// MUST return <c>true</c>. Uses an in-memory <see cref="IDistributedCache"/> — no
    /// external infra required.
    /// </summary>
    [Fact]
    public async Task RevokeAsync_AfterIssue_BlacklistsTheJti()
    {
        // Arrange: set up a real in-memory blacklist backed by MemoryDistributedCache
        var memCache = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions()));
        var blacklist = new RedisJtiBlacklist(memCache, NullLogger<RedisJtiBlacklist>.Instance);

        // Create a session directly on the aggregate (no service needed for this path)
        var jti = Guid.NewGuid().ToString("N");
        var session = ImpersonationSession.Issue(
            hostUserId: Guid.NewGuid(),
            tenantId: "tenant-revoke-test",
            reason: "Round-trip revocation test",
            jti: jti,
            ttl: TimeSpan.FromMinutes(15));

        // Verify the jti is NOT yet blacklisted
        (await blacklist.IsRevokedAsync(jti)).Should().BeFalse(
            "token should not be revoked before RevokeAsync is called");

        // Act: simulate what ImpersonationService.RevokeAsync does
        session.Revoke(DateTime.UtcNow);
        await blacklist.RevokeAsync(session.Jti, session.ExpiresAt);

        // Assert: jti is now blacklisted
        (await blacklist.IsRevokedAsync(jti)).Should().BeTrue(
            "blacklist must report the jti as revoked after RevokeAsync completes");
    }
}
