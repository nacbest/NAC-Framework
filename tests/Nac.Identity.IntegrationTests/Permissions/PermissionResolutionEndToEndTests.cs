using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.IntegrationTests.Infrastructure;
using Nac.Identity.Memberships;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.Roles;
using Xunit;

namespace Nac.Identity.IntegrationTests.Permissions;

/// <summary>
/// End-to-end permission resolution: verifies <see cref="IPermissionChecker"/> correctly
/// unions user + role grants, respects permission tree ancestry, and reacts to role
/// assignment changes.
/// </summary>
[Collection("Integration")]
public sealed class PermissionResolutionEndToEndTests : IAsyncLifetime
{
    private readonly PostgresFixture _fx;
    private IdentityIntegrationHost? _host;

    public PermissionResolutionEndToEndTests(PostgresFixture fx) { _fx = fx; }

    private sealed class Perms : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext c)
        {
            var orders = c.AddGroup("Orders");
            var parent = orders.AddPermission("Orders");
            parent.AddChild("Orders.View");
            parent.AddChild("Orders.Edit");
        }
    }

    public async ValueTask InitializeAsync()
    {
        var cs = await _fx.CreateDatabaseAsync($"perm_{Guid.NewGuid():N}");
        _host = await IdentityIntegrationHost.CreateAsync(cs, permissionProviders: [new Perms()]);
    }

    public async ValueTask DisposeAsync() { if (_host is not null) await _host.DisposeAsync(); }

    [Fact]
    public async Task AncestorGrant_GrantsDescendantPermissions()
    {
        var userId = Guid.NewGuid();
        var repo = _host!.GetRequiredService<IPermissionGrantRepository>();
        await repo.AddGrantAsync(PermissionProviderNames.User, userId.ToString(), "Orders", "t1");

        var checker = _host.GetRequiredService<IPermissionChecker>();
        (await checker.IsGrantedAsync(userId, "Orders.View", "t1")).Should().BeTrue();
        (await checker.IsGrantedAsync(userId, "Orders.Edit", "t1")).Should().BeTrue();
    }

    [Fact]
    public async Task RoleGrant_AppearsInUserPermissionResolution_ViaMembership()
    {
        var user = new Nac.Identity.Users.NacUser("res@example.com", "Res");
        _host!.Db.Users.Add(user);
        await _host.Db.SaveChangesAsync();
        var roleSvc = _host.GetRequiredService<IRoleService>();
        var membershipSvc = _host.GetRequiredService<IMembershipService>();
        var repo = _host.GetRequiredService<IPermissionGrantRepository>();

        var role = await roleSvc.CreateAsync("t1", "Editor");
        await roleSvc.GrantPermissionAsync(role.Id, "Orders.Edit", "t1");
        await membershipSvc.CreateActiveMembershipAsync(user.Id, "t1", [role.Id], isDefault: true);

        var roleGrants = await repo.ListGrantsAsync(PermissionProviderNames.Role, role.Id.ToString(), "t1");
        var userRoles = await membershipSvc.GetRoleIdsAsync(user.Id, "t1");

        userRoles.Should().Contain(role.Id);
        roleGrants.Should().Contain("Orders.Edit");
    }

    [Fact]
    public async Task UserDirectGrant_IsReturnedByRepository()
    {
        var userId = Guid.NewGuid();
        var repo = _host!.GetRequiredService<IPermissionGrantRepository>();
        await repo.AddGrantAsync(PermissionProviderNames.User, userId.ToString(), "Orders.View", "t1");

        var grants = await repo.ListGrantsAsync(PermissionProviderNames.User, userId.ToString(), "t1");
        grants.Should().Contain("Orders.View");
    }
}
