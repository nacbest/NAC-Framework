using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Nac.Core.Abstractions.Identity;
using Nac.Core.Abstractions.Permissions;
using Nac.Core.Domain;
using Nac.Identity.Jwt;
using Nac.Identity.Permissions.Host;
using Nac.Identity.Users;

namespace Nac.Identity.Impersonation;

/// <summary>
/// Default <see cref="IImpersonationService"/>. Enforces the security invariants laid
/// out in the plan (self-only, no nesting, runtime permission check, atomic rate limit,
/// <c>is_host=false</c> in minted token). Token TTL is fixed at 15 minutes; there is
/// no refresh — re-impersonating is a fresh audit event.
/// </summary>
internal sealed partial class ImpersonationService(
    UserManager<NacUser> users,
    IImpersonationSessionRepository sessions,
    JwtTokenService jwt,
    IJtiBlacklist blacklist,
    IImpersonationRoleProvider roleProvider,
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser,
    IImpersonationRateLimiter rateLimiter) : IImpersonationService
{
    private static readonly TimeSpan TokenTtl = TimeSpan.FromMinutes(15);

    public async Task<ImpersonationIssueResult> IssueAsync(
        Guid hostUserId, string tenantId, string reason, CancellationToken ct = default)
    {
        // Self-only: the service never accepts an arbitrary actor id.
        if (hostUserId != currentUser.Id)
            throw new ForbiddenAccessException("Impersonation is self-only (hostUserId must equal current user).");

        // Nested-impersonation guard: a caller already running under an `act` claim cannot re-impersonate.
        if (currentUser.ImpersonatorId is not null)
            throw new ForbiddenAccessException("Nested impersonation is not supported.");

        if (!currentUser.IsHost ||
            !await permissionChecker.IsGrantedAsync(HostPermissions.ImpersonateTenant, ct))
            throw new ForbiddenAccessException("Missing Host.ImpersonateTenant permission.");

        ValidateReason(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        if (!await rateLimiter.TryConsumeAsync(hostUserId, ct))
            throw new ImpersonationRateLimitExceededException();

        var host = await users.FindByIdAsync(hostUserId.ToString())
                   ?? throw new ForbiddenAccessException("Host user not found.");

        var template = await roleProvider.GetImpersonationRoleAsync(tenantId, ct);

        var jti = Guid.NewGuid().ToString("N");
        var tokenResult = jwt.GenerateImpersonationToken(
            subjectUserId: hostUserId,
            tenantId: tenantId,
            email: host.Email ?? string.Empty,
            name: host.FullName,
            roleIds: template.RoleIds.ToArray(),
            actorUserId: hostUserId,
            jti: jti,
            ttl: TokenTtl);

        var session = ImpersonationSession.Issue(hostUserId, tenantId, reason.Trim(), jti, TokenTtl);
        await sessions.AddAsync(session, ct);
        await sessions.SaveChangesAsync(ct);

        return new ImpersonationIssueResult(tokenResult.Token, session);
    }

    public async Task RevokeAsync(Guid sessionId, Guid callerUserId, CancellationToken ct = default)
    {
        var session = await sessions.GetByIdAsync(sessionId, ct)
                      ?? throw new KeyNotFoundException($"Impersonation session {sessionId} not found.");

        var isOwner = session.HostUserId == callerUserId;
        var hasRevokePermission = await permissionChecker.IsGrantedAsync(HostPermissions.ImpersonateTenant, ct);
        if (!isOwner && !hasRevokePermission)
            throw new ForbiddenAccessException("Caller cannot revoke this impersonation session.");

        session.Revoke(DateTime.UtcNow);
        await sessions.SaveChangesAsync(ct);
        await blacklist.RevokeAsync(session.Jti, session.ExpiresAt, ct);
    }

    public Task<IReadOnlyList<ImpersonationSession>> ListByTenantAsync(
        string tenantId, int skip, int take, CancellationToken ct = default) =>
        sessions.ListByTenantAsync(tenantId, skip, take, ct);

    private static void ValidateReason(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        var trimmed = reason.Trim();
        if (trimmed.Length < 10 || trimmed.Length > 500)
            throw new ArgumentException("Reason must be 10–500 characters.", nameof(reason));
        if (!ReasonRegex().IsMatch(trimmed))
            throw new ArgumentException("Reason contains disallowed characters.", nameof(reason));
    }

    [GeneratedRegex(@"^[\w\s\-#:.,()/]+$")]
    private static partial Regex ReasonRegex();
}
