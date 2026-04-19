using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Nac.Identity.Memberships;
using Nac.Identity.Permissions.Cache;
using Nac.Identity.Tests.Infrastructure;
using Xunit;

namespace Nac.Identity.Tests.Memberships;

public class MembershipServiceTests
{
    private static (MembershipService service, TestIdentityDbContext db, RecordingPermissionGrantCache cache) Build()
    {
        var host = IdentityTestHost.Create();
        var cache = new RecordingPermissionGrantCache();
        var svc = new MembershipService(host.Db, cache);
        return (svc, host.Db, cache);
    }

    [Fact]
    public async Task InviteAsync_CreatesInvitedMembership_WithRoles()
    {
        var (svc, db, _) = Build();
        var userId = Guid.NewGuid();
        var inviter = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var (id, token) = await svc.InviteAsync(userId, "t1", inviter, [roleId]);

        token.Should().NotBeNullOrWhiteSpace();
        var m = await db.Memberships.Include(x => x.Roles).FirstAsync(x => x.Id == id);
        m.Status.Should().Be(MembershipStatus.Invited);
        m.UserId.Should().Be(userId);
        m.TenantId.Should().Be("t1");
        m.InvitedBy.Should().Be(inviter);
        m.Roles.Select(r => r.RoleId).Should().ContainSingle().Which.Should().Be(roleId);
    }

    [Fact]
    public async Task AcceptAsync_ActivatesInvitedMembership_AndInvalidatesCache()
    {
        var (svc, db, cache) = Build();
        var userId = Guid.NewGuid();
        var (id, _) = await svc.InviteAsync(userId, "t1", Guid.NewGuid(), []);

        await svc.AcceptAsync("ignored", userId);

        var m = await db.Memberships.FirstAsync(x => x.Id == id);
        m.Status.Should().Be(MembershipStatus.Active);
        m.JoinedAt.Should().NotBeNull();
        cache.Invalidated.Should().Contain(PermissionCacheKeys.User(userId, "t1"));
    }

    [Fact]
    public async Task AcceptAsync_WithNoPendingInvite_Throws()
    {
        var (svc, _, _) = Build();
        var act = async () => await svc.AcceptAsync("x", Guid.NewGuid());
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateActiveMembershipAsync_SetsActive_WithJoinedAt_AndInvalidatesCache()
    {
        var (svc, db, cache) = Build();
        var userId = Guid.NewGuid();

        var id = await svc.CreateActiveMembershipAsync(userId, "t1", [Guid.NewGuid(), Guid.NewGuid()], isDefault: true);

        var m = await db.Memberships.Include(x => x.Roles).FirstAsync(x => x.Id == id);
        m.Status.Should().Be(MembershipStatus.Active);
        m.IsDefault.Should().BeTrue();
        m.JoinedAt.Should().NotBeNull();
        m.Roles.Should().HaveCount(2);
        cache.Invalidated.Should().Contain(PermissionCacheKeys.User(userId, "t1"));
    }

    [Fact]
    public async Task ChangeRolesAsync_ReplacesRoles_AndInvalidatesCache()
    {
        var (svc, db, cache) = Build();
        var userId = Guid.NewGuid();
        var roleA = Guid.NewGuid();
        var roleB = Guid.NewGuid();
        var id = await svc.CreateActiveMembershipAsync(userId, "t1", [roleA], false);
        cache.Invalidated.Clear();

        await svc.ChangeRolesAsync(id, [roleB]);

        var m = await db.Memberships.Include(x => x.Roles).FirstAsync(x => x.Id == id);
        m.Roles.Select(r => r.RoleId).Should().BeEquivalentTo([roleB]);
        cache.Invalidated.Should().Contain(PermissionCacheKeys.User(userId, "t1"));
    }

    [Fact]
    public async Task RemoveMemberAsync_SoftDeletes_AndInvalidatesCache()
    {
        var (svc, db, cache) = Build();
        var userId = Guid.NewGuid();
        var id = await svc.CreateActiveMembershipAsync(userId, "t1", [], false);
        cache.Invalidated.Clear();

        await svc.RemoveMemberAsync(id);

        // Soft-deleted rows are filtered by the query filter, so IgnoreQueryFilters() is needed.
        var m = await db.Memberships.IgnoreQueryFilters().FirstAsync(x => x.Id == id);
        m.IsDeleted.Should().BeTrue();
        m.Status.Should().Be(MembershipStatus.Removed);
        cache.Invalidated.Should().Contain(PermissionCacheKeys.User(userId, "t1"));
    }

    [Fact]
    public async Task GetRoleIdsAsync_ReturnsOnlyActiveMembershipRoles()
    {
        var (svc, _, _) = Build();
        var userId = Guid.NewGuid();
        var activeRole = Guid.NewGuid();
        var invitedRole = Guid.NewGuid();
        await svc.CreateActiveMembershipAsync(userId, "t1", [activeRole], false);
        await svc.InviteAsync(userId, "t2", Guid.NewGuid(), [invitedRole]);

        var t1Roles = await svc.GetRoleIdsAsync(userId, "t1");
        var t2Roles = await svc.GetRoleIdsAsync(userId, "t2");

        t1Roles.Should().ContainSingle().Which.Should().Be(activeRole);
        t2Roles.Should().BeEmpty("invited memberships are not yet active");
    }

    [Fact]
    public async Task ListForUserAsync_ReturnsMembershipsAcrossTenants()
    {
        var (svc, _, _) = Build();
        var userId = Guid.NewGuid();
        await svc.CreateActiveMembershipAsync(userId, "t1", [], false);
        await svc.CreateActiveMembershipAsync(userId, "t2", [], false);

        var result = await svc.ListForUserAsync(userId);

        result.Select(m => m.TenantId).Should().BeEquivalentTo(["t1", "t2"]);
    }

    [Fact]
    public async Task SetDefaultAsync_MakesOnlyTargetMembershipDefault()
    {
        var (svc, _, _) = Build();
        var userId = Guid.NewGuid();
        await svc.CreateActiveMembershipAsync(userId, "t1", [], true);
        await svc.CreateActiveMembershipAsync(userId, "t2", [], false);

        await svc.SetDefaultAsync(userId, "t2");

        var memberships = await svc.ListForUserAsync(userId);
        memberships.Single(m => m.TenantId == "t1").IsDefault.Should().BeFalse();
        memberships.Single(m => m.TenantId == "t2").IsDefault.Should().BeTrue();
    }
}
