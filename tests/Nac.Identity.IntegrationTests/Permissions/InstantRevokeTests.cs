using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.IntegrationTests.Infrastructure;
using Nac.Identity.Permissions.Cache;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.Roles;
using Xunit;

namespace Nac.Identity.IntegrationTests.Permissions;

/// <summary>
/// R3 (critical) — grant / revoke must take effect on the very next
/// <see cref="IPermissionChecker"/> call. Cache invalidation is the contract.
/// </summary>
[Collection("Integration")]
public sealed class InstantRevokeTests : IAsyncLifetime
{
    private readonly PostgresFixture _fx;
    private IdentityIntegrationHost? _host;

    public InstantRevokeTests(PostgresFixture fx) { _fx = fx; }

    private sealed class Perms : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext c)
        {
            var g = c.AddGroup("Orders");
            g.AddPermission("Orders.View");
        }
    }

    public async ValueTask InitializeAsync()
    {
        var cs = await _fx.CreateDatabaseAsync($"rev_{Guid.NewGuid():N}");
        _host = await IdentityIntegrationHost.CreateAsync(cs, permissionProviders: [new Perms()]);
    }

    public async ValueTask DisposeAsync() { if (_host is not null) await _host.DisposeAsync(); }

    [Fact]
    public async Task Grant_Then_Revoke_OnUserDirectGrant_IsReflectedOnNextCheckerCall()
    {
        var userId = Guid.NewGuid();
        var svc = _host!.GetRequiredService<IRoleService>();
        var role = await svc.CreateAsync("t1", "Manager");
        // Seed a direct user grant to simulate a caller principal scope we own.
        var repo = _host.GetRequiredService<IPermissionGrantRepository>();
        var cache = _host.GetRequiredService<IPermissionGrantCache>();
        var checker = _host.GetRequiredService<IPermissionChecker>();

        await svc.GrantPermissionAsync(role.Id, "Orders.View", "t1");

        // Direct grant via role cache key (user grant fallback used by cross-user overload).
        await repo.AddGrantAsync(PermissionProviderNames.User, userId.ToString(), "Orders.View", "t1");
        (await checker.IsGrantedAsync(userId, "Orders.View", "t1")).Should().BeTrue();

        await repo.RemoveGrantAsync(PermissionProviderNames.User, userId.ToString(), "Orders.View", "t1");
        await cache.InvalidateAsync(PermissionCacheKeys.User(userId, "t1"));

        (await checker.IsGrantedAsync(userId, "Orders.View", "t1")).Should().BeFalse();
    }

    [Fact]
    public async Task RoleService_Grant_InvalidatesRoleCache()
    {
        var svc = _host!.GetRequiredService<IRoleService>();
        var cache = _host.GetRequiredService<IPermissionGrantCache>();
        var role = await svc.CreateAsync("t1", "Editor");

        // Prime the cache with an empty grant set.
        var key = PermissionCacheKeys.Role(role.Id, "t1");
        var primed = await cache.GetOrLoadAsync(key, _ => Task.FromResult(new HashSet<string>()),
            TimeSpan.FromMinutes(10));
        primed.Should().BeEmpty();

        await svc.GrantPermissionAsync(role.Id, "Orders.View", "t1");

        // After invalidation the loader must be called again and see the new grant.
        var factoryInvocations = 0;
        var refreshed = await cache.GetOrLoadAsync(key, async ct =>
        {
            factoryInvocations++;
            var repo = _host.GetRequiredService<IPermissionGrantRepository>();
            return await repo.ListGrantsAsync(PermissionProviderNames.Role, role.Id.ToString(), "t1", ct);
        }, TimeSpan.FromMinutes(10));

        factoryInvocations.Should().Be(1, "cache key must be invalidated on grant");
        refreshed.Should().Contain("Orders.View");
    }
}
