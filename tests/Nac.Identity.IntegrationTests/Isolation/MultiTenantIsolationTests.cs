using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.IntegrationTests.Infrastructure;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.Roles;
using Nac.Identity.Users;
using Xunit;

namespace Nac.Identity.IntegrationTests.Isolation;

/// <summary>
/// R1 (critical) — grants in one tenant MUST NOT satisfy checks in another. Validated
/// at the repository level (source of truth) and via <see cref="IPermissionChecker"/>.
/// </summary>
[Collection("Integration")]
public sealed class MultiTenantIsolationTests : IAsyncLifetime
{
    private readonly PostgresFixture _fx;
    private IdentityIntegrationHost? _host;

    public MultiTenantIsolationTests(PostgresFixture fx) { _fx = fx; }

    private sealed class Perms : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext context)
        {
            var g = context.AddGroup("Orders");
            g.AddPermission("Orders.View");
            g.AddPermission("Orders.Edit");
        }
    }

    public async ValueTask InitializeAsync()
    {
        var cs = await _fx.CreateDatabaseAsync($"iso_{Guid.NewGuid():N}");
        _host = await IdentityIntegrationHost.CreateAsync(cs, permissionProviders: [new Perms()]);
    }

    public async ValueTask DisposeAsync() { if (_host is not null) await _host.DisposeAsync(); }

    [Fact]
    public async Task RoleGrant_InTenantA_DoesNotAppearInTenantB()
    {
        var svc = _host!.GetRequiredService<IRoleService>();
        var roleA = await svc.CreateAsync("tenant-a", "Manager");
        await svc.GrantPermissionAsync(roleA.Id, "Orders.View", "tenant-a");

        var repo = _host.GetRequiredService<IPermissionGrantRepository>();
        var grantsA = await repo.ListGrantsAsync(PermissionProviderNames.Role, roleA.Id.ToString(), "tenant-a");
        var grantsB = await repo.ListGrantsAsync(PermissionProviderNames.Role, roleA.Id.ToString(), "tenant-b");

        grantsA.Should().Contain("Orders.View");
        grantsB.Should().BeEmpty("grant is scoped to tenant-a only");
    }

    [Fact]
    public async Task UserGrant_InTenantA_DoesNotSatisfyCrossTenantCheck()
    {
        var userId = Guid.NewGuid();
        var repo = _host!.GetRequiredService<IPermissionGrantRepository>();
        await repo.AddGrantAsync(PermissionProviderNames.User, userId.ToString(), "Orders.View", "tenant-a");

        var checker = _host.GetRequiredService<IPermissionChecker>();
        (await checker.IsGrantedAsync(userId, "Orders.View", "tenant-a")).Should().BeTrue();
        (await checker.IsGrantedAsync(userId, "Orders.View", "tenant-b")).Should().BeFalse();
    }

    [Fact]
    public async Task TenantScopedRoles_AreIsolatedByTenantId()
    {
        var svc = _host!.GetRequiredService<IRoleService>();
        var roleA = await svc.CreateAsync("tenant-a", "Manager-A");
        var roleB = await svc.CreateAsync("tenant-b", "Manager-B");

        var listA = await svc.ListForTenantAsync("tenant-a");
        var listB = await svc.ListForTenantAsync("tenant-b");

        listA.Should().ContainSingle().Which.Id.Should().Be(roleA.Id);
        listB.Should().ContainSingle().Which.Id.Should().Be(roleB.Id);
    }

    [Fact]
    public async Task NacUser_SoftDelete_ExcludedByQueryFilter()
    {
        var db = _host!.Db;
        var user = new NacUser("filter@example.com", "Filter") { IsDeleted = true };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var visible = db.Users.Where(u => u.Id == user.Id).ToList();
        var unfiltered = db.Users.IgnoreQueryFilters().Where(u => u.Id == user.Id).ToList();

        visible.Should().BeEmpty("soft-delete filter must hide deleted rows");
        unfiltered.Should().HaveCount(1);
    }
}
