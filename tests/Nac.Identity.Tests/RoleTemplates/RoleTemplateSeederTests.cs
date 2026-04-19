using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Permissions;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.RoleTemplates;
using Nac.Identity.Tests.Infrastructure;
using Xunit;

namespace Nac.Identity.Tests.RoleTemplates;

public class RoleTemplateSeederTests
{
    private sealed class PermissionProvider : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext context)
        {
            var g = context.AddGroup("Orders");
            g.AddPermission("Orders.View");
            g.AddPermission("Orders.Edit");
            g.AddPermission("Orders.Delete");
        }
    }

    private sealed class TemplateProvider : IRoleTemplateProvider
    {
        private readonly Action<IRoleTemplateContext> _define;
        public TemplateProvider(Action<IRoleTemplateContext> define) { _define = define; }
        public void Define(IRoleTemplateContext context) => _define(context);
    }

    private static async Task<(RoleTemplateSeeder seeder, IdentityTestHost host, RoleTemplateDefinitionManager mgr)>
        BuildAsync(Action<IRoleTemplateContext> defineTemplates)
    {
        var host = IdentityTestHost.Create(
            permissionProviders: [new PermissionProvider()]);
        var mgr = new RoleTemplateDefinitionManager([new TemplateProvider(defineTemplates)]);
        var seeder = new RoleTemplateSeeder(
            host.GetRequiredService<IServiceScopeFactory>(),
            mgr,
            NullLogger<RoleTemplateSeeder>.Instance);
        await Task.CompletedTask;
        return (seeder, host, mgr);
    }

    [Fact]
    public async Task StartAsync_InsertsTemplateRoles_WithStableIds()
    {
        var (seeder, host, _) = await BuildAsync(ctx =>
        {
            ctx.AddTemplate("owner", "Owner").Grants("Orders.View", "Orders.Edit");
        });

        await seeder.StartAsync(CancellationToken.None);

        var expectedId = RoleTemplateKeyHasher.ToGuid("owner");
        var role = await host.Db.Roles.FirstOrDefaultAsync(r => r.Id == expectedId);
        role.Should().NotBeNull();
        role!.IsTemplate.Should().BeTrue();
        role.TenantId.Should().BeNull();
        var grants = await host.Db.PermissionGrants
            .Where(g => g.ProviderName == PermissionProviderNames.Role && g.ProviderKey == expectedId.ToString())
            .Select(g => g.PermissionName).ToListAsync();
        grants.Should().BeEquivalentTo(["Orders.View", "Orders.Edit"]);
    }

    [Fact]
    public async Task StartAsync_SecondRun_IsIdempotent()
    {
        var (seeder, host, _) = await BuildAsync(ctx =>
        {
            ctx.AddTemplate("owner", "Owner").Grants("Orders.View");
        });

        await seeder.StartAsync(CancellationToken.None);
        await seeder.StartAsync(CancellationToken.None);

        (await host.Db.Roles.CountAsync(r => r.IsTemplate)).Should().Be(1);
        (await host.Db.PermissionGrants.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task StartAsync_WhenProviderAddsNewGrant_SeederInsertsIt()
    {
        var host = IdentityTestHost.Create(permissionProviders: [new PermissionProvider()]);

        var mgrInitial = new RoleTemplateDefinitionManager([new TemplateProvider(ctx =>
            ctx.AddTemplate("owner", "Owner").Grants("Orders.View"))]);
        await new RoleTemplateSeeder(
            host.GetRequiredService<IServiceScopeFactory>(),
            mgrInitial,
            NullLogger<RoleTemplateSeeder>.Instance).StartAsync(CancellationToken.None);

        var mgrUpdated = new RoleTemplateDefinitionManager([new TemplateProvider(ctx =>
            ctx.AddTemplate("owner", "Owner").Grants("Orders.View", "Orders.Edit"))]);
        await new RoleTemplateSeeder(
            host.GetRequiredService<IServiceScopeFactory>(),
            mgrUpdated,
            NullLogger<RoleTemplateSeeder>.Instance).StartAsync(CancellationToken.None);

        var expectedId = RoleTemplateKeyHasher.ToGuid("owner");
        var grants = await host.Db.PermissionGrants
            .Where(g => g.ProviderKey == expectedId.ToString())
            .Select(g => g.PermissionName).ToListAsync();
        grants.Should().BeEquivalentTo(["Orders.View", "Orders.Edit"]);
    }

    [Fact]
    public async Task StartAsync_WhenProviderRemovesGrant_SeederRemovesStale()
    {
        var host = IdentityTestHost.Create(permissionProviders: [new PermissionProvider()]);

        var mgrInitial = new RoleTemplateDefinitionManager([new TemplateProvider(ctx =>
            ctx.AddTemplate("owner", "Owner").Grants("Orders.View", "Orders.Edit", "Orders.Delete"))]);
        await new RoleTemplateSeeder(
            host.GetRequiredService<IServiceScopeFactory>(),
            mgrInitial,
            NullLogger<RoleTemplateSeeder>.Instance).StartAsync(CancellationToken.None);

        var mgrUpdated = new RoleTemplateDefinitionManager([new TemplateProvider(ctx =>
            ctx.AddTemplate("owner", "Owner").Grants("Orders.View"))]);
        await new RoleTemplateSeeder(
            host.GetRequiredService<IServiceScopeFactory>(),
            mgrUpdated,
            NullLogger<RoleTemplateSeeder>.Instance).StartAsync(CancellationToken.None);

        var expectedId = RoleTemplateKeyHasher.ToGuid("owner");
        var grants = await host.Db.PermissionGrants
            .Where(g => g.ProviderKey == expectedId.ToString())
            .Select(g => g.PermissionName).ToListAsync();
        grants.Should().BeEquivalentTo(["Orders.View"]);
    }

    [Fact]
    public async Task StartAsync_SkipsUnknownPermissions_WithoutFailing()
    {
        var (seeder, host, _) = await BuildAsync(ctx =>
        {
            ctx.AddTemplate("owner", "Owner").Grants("Orders.View", "Not.A.Real.Permission");
        });

        await seeder.StartAsync(CancellationToken.None);

        var expectedId = RoleTemplateKeyHasher.ToGuid("owner");
        var grants = await host.Db.PermissionGrants
            .Where(g => g.ProviderKey == expectedId.ToString())
            .Select(g => g.PermissionName).ToListAsync();
        grants.Should().BeEquivalentTo(["Orders.View"]);
    }

    [Fact]
    public void RoleTemplateKeyHasher_ProducesStableGuid_AcrossCalls()
    {
        RoleTemplateKeyHasher.ToGuid("owner").Should().Be(RoleTemplateKeyHasher.ToGuid("owner"));
        RoleTemplateKeyHasher.ToGuid("owner").Should().NotBe(RoleTemplateKeyHasher.ToGuid("admin"));
    }
}
