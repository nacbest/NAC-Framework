using Microsoft.EntityFrameworkCore;
using Nac.Identity.Context;
using Nac.Identity.Permissions.Cache;

namespace Nac.Identity.Memberships;

/// <summary>
/// EF Core-backed <see cref="IMembershipService"/>. Role-assignment mutations invalidate
/// the affected user's permission cache key to guarantee instant revoke semantics.
/// </summary>
internal sealed class MembershipService(
    NacIdentityDbContext db,
    IPermissionGrantCache permissionCache) : IMembershipService
{
    public async Task<(Guid membershipId, string inviteToken)> InviteAsync(
        Guid userId, string tenantId, Guid invitedBy, IReadOnlyList<Guid> roleIds,
        CancellationToken ct = default)
    {
        var membership = new UserTenantMembership(userId, tenantId,
            MembershipStatus.Invited, invitedBy);
        db.Memberships.Add(membership);

        foreach (var roleId in roleIds)
            db.MembershipRoles.Add(new MembershipRole(membership.Id, roleId));

        await db.SaveChangesAsync(ct);

        // Opaque token — in v3 we use a plain Guid; Phase 05 may swap to a signed token.
        var inviteToken = Guid.NewGuid().ToString("N");
        return (membership.Id, inviteToken);
    }

    public async Task AcceptAsync(string inviteToken, Guid userId, CancellationToken ct = default)
    {
        // v3: token lookup is handled by the invite endpoint (Phase 03); here we just flip status
        // on the most recent Invited membership for this user. A later phase wires durable tokens.
        var pending = await db.Memberships
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Invited)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (pending is null)
            throw new InvalidOperationException("No pending invitation found for this user.");

        pending.Activate();
        await db.SaveChangesAsync(ct);

        await InvalidateUserCacheAsync(pending.UserId, pending.TenantId, ct);
    }

    public async Task<Guid> CreateActiveMembershipAsync(Guid userId, string tenantId,
                                                       IReadOnlyList<Guid> roleIds, bool isDefault,
                                                       CancellationToken ct = default)
    {
        var membership = new UserTenantMembership(userId, tenantId,
            MembershipStatus.Active, invitedBy: null, isDefault: isDefault);
        db.Memberships.Add(membership);

        foreach (var roleId in roleIds)
            db.MembershipRoles.Add(new MembershipRole(membership.Id, roleId));

        await db.SaveChangesAsync(ct);
        await InvalidateUserCacheAsync(userId, tenantId, ct);
        return membership.Id;
    }

    public async Task<IReadOnlyList<MembershipSummary>> ListForUserAsync(Guid userId,
                                                                        CancellationToken ct = default)
    {
        var memberships = await db.Memberships
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Include(m => m.Roles)
            .ToListAsync(ct);

        return memberships.Select(Project).ToList();
    }

    public async Task<IReadOnlyList<MembershipSummary>> ListMembersAsync(string tenantId,
                                                                        CancellationToken ct = default)
    {
        var memberships = await db.Memberships
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .Include(m => m.Roles)
            .ToListAsync(ct);

        return memberships.Select(Project).ToList();
    }

    public async Task ChangeRolesAsync(Guid membershipId, IReadOnlyList<Guid> roleIds,
                                       CancellationToken ct = default)
    {
        var membership = await db.Memberships
            .Include(m => m.Roles)
            .FirstOrDefaultAsync(m => m.Id == membershipId, ct)
            ?? throw new InvalidOperationException($"Membership {membershipId} not found.");

        db.MembershipRoles.RemoveRange(membership.Roles);
        foreach (var roleId in roleIds)
            db.MembershipRoles.Add(new MembershipRole(membership.Id, roleId));

        await db.SaveChangesAsync(ct);
        await InvalidateUserCacheAsync(membership.UserId, membership.TenantId, ct);
    }

    public async Task RemoveMemberAsync(Guid membershipId, CancellationToken ct = default)
    {
        var membership = await db.Memberships
            .FirstOrDefaultAsync(m => m.Id == membershipId, ct)
            ?? throw new InvalidOperationException($"Membership {membershipId} not found.");

        membership.Status = MembershipStatus.Removed;
        membership.IsDeleted = true;
        membership.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await InvalidateUserCacheAsync(membership.UserId, membership.TenantId, ct);
    }

    public async Task SetDefaultAsync(Guid userId, string tenantId, CancellationToken ct = default)
    {
        var memberships = await db.Memberships
            .Where(m => m.UserId == userId)
            .ToListAsync(ct);

        foreach (var m in memberships)
            m.IsDefault = string.Equals(m.TenantId, tenantId, StringComparison.Ordinal);

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> GetRoleIdsAsync(Guid userId, string tenantId,
                                                          CancellationToken ct = default)
    {
        return await db.Memberships
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.TenantId == tenantId
                     && m.Status == MembershipStatus.Active)
            .SelectMany(m => m.Roles.Select(r => r.RoleId))
            .ToListAsync(ct);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private Task InvalidateUserCacheAsync(Guid userId, string tenantId, CancellationToken ct) =>
        permissionCache.InvalidateAsync(PermissionCacheKeys.User(userId, tenantId), ct);

    private static MembershipSummary Project(UserTenantMembership m) =>
        new(m.Id, m.UserId, m.TenantId, m.Status,
            m.Roles.Select(r => r.RoleId).ToList(),
            m.IsDefault, m.JoinedAt);
}
