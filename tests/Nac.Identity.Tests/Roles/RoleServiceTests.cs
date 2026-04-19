using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Permissions;
using Nac.Identity.Permissions.Cache;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.Roles;
using Nac.Identity.Tests.Infrastructure;
using Nac.Identity.Users;
using Xunit;

namespace Nac.Identity.Tests.Roles;

public class RoleServiceTests
{
    private sealed class TestPermissions : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext context)
        {
            var orders = context.AddGroup("Orders");
            orders.AddPermission("Orders.View");
            orders.AddPermission("Orders.Edit");
            var users = context.AddGroup("Users");
            users.AddPermission("Users.Read");
        }
    }

    private static async Task<(RoleService svc, IdentityTestHost host, RecordingPermissionGrantCache cache, IPermissionGrantRepository repo)> BuildAsync()
    {
        var host = IdentityTestHost.Create(
            permissionProviders: [new TestPermissions()]);
        var cache = new RecordingPermissionGrantCache();
        var repo = new EfCorePermissionGrantRepository(host.Db);
        var svc = new RoleService(
            host.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<NacRole>>(),
            host.Db, repo, cache, host.GetRequiredService<PermissionDefinitionManager>());
        await Task.CompletedTask;
        return (svc, host, cache, repo);
    }

    [Fact]
    public async Task CreateAsync_PersistsTenantRole()
    {
        var (svc, host, _, _) = await BuildAsync();

        var role = await svc.CreateAsync("t1", "Manager", "tenant manager");

        role.TenantId.Should().Be("t1");
        role.IsTemplate.Should().BeFalse();
        (await host.Db.Roles.CountAsync(r => r.Id == role.Id)).Should().Be(1);
    }

    [Fact]
    public async Task CloneFromTemplateAsync_CopiesGrants_ToNewRole()
    {
        var (svc, host, _, repo) = await BuildAsync();
        var template = new NacRole("Admin", tenantId: null, isTemplate: true);
        host.Db.Roles.Add(template);
        await host.Db.SaveChangesAsync();
        await repo.AddGrantAsync(PermissionProviderNames.Role, template.Id.ToString(), "Orders.View", null);
        await repo.AddGrantAsync(PermissionProviderNames.Role, template.Id.ToString(), "Orders.Edit", null);

        var clone = await svc.CloneFromTemplateAsync("t1", template.Id, newName: "TenantAdmin");

        clone.TenantId.Should().Be("t1");
        clone.IsTemplate.Should().BeFalse();
        clone.BaseTemplateId.Should().Be(template.Id);
        var grants = await repo.ListGrantsAsync(PermissionProviderNames.Role, clone.Id.ToString(), "t1");
        grants.Should().BeEquivalentTo(["Orders.View", "Orders.Edit"]);
    }

    [Fact]
    public async Task CloneFromTemplateAsync_NonTemplateSource_Throws()
    {
        var (svc, host, _, _) = await BuildAsync();
        var custom = new NacRole("Custom", tenantId: "t1", isTemplate: false);
        host.Db.Roles.Add(custom);
        await host.Db.SaveChangesAsync();

        var act = async () => await svc.CloneFromTemplateAsync("t2", custom.Id);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CloneFromTemplateAsync_MissingTemplate_Throws()
    {
        var (svc, _, _, _) = await BuildAsync();
        var act = async () => await svc.CloneFromTemplateAsync("t1", Guid.NewGuid());
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GrantPermissionAsync_OnTemplate_Rejected()
    {
        var (svc, host, _, _) = await BuildAsync();
        var template = new NacRole("Admin", tenantId: null, isTemplate: true);
        host.Db.Roles.Add(template);
        await host.Db.SaveChangesAsync();

        var act = async () => await svc.GrantPermissionAsync(template.Id, "Orders.View", "t1");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GrantPermissionAsync_UnknownRole_Throws()
    {
        var (svc, _, _, _) = await BuildAsync();
        var act = async () => await svc.GrantPermissionAsync(Guid.NewGuid(), "Orders.View", "t1");
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GrantPermissionAsync_UnknownPermission_ThrowsArgument()
    {
        var (svc, host, _, _) = await BuildAsync();
        var role = new NacRole("R1", tenantId: "t1", isTemplate: false);
        host.Db.Roles.Add(role);
        await host.Db.SaveChangesAsync();

        var act = async () => await svc.GrantPermissionAsync(role.Id, "Not.Defined", "t1");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GrantPermissionAsync_Success_PersistsAndInvalidatesRoleCache()
    {
        var (svc, host, cache, repo) = await BuildAsync();
        var role = new NacRole("R1", tenantId: "t1", isTemplate: false);
        host.Db.Roles.Add(role);
        await host.Db.SaveChangesAsync();

        await svc.GrantPermissionAsync(role.Id, "Orders.View", "t1");

        (await repo.ListGrantsAsync(PermissionProviderNames.Role, role.Id.ToString(), "t1"))
            .Should().Contain("Orders.View");
        cache.Invalidated.Should().Contain(PermissionCacheKeys.Role(role.Id, "t1"));
    }

    [Fact]
    public async Task RevokePermissionAsync_RemovesGrant_AndInvalidates()
    {
        var (svc, host, cache, repo) = await BuildAsync();
        var role = new NacRole("R1", tenantId: "t1", isTemplate: false);
        host.Db.Roles.Add(role);
        await host.Db.SaveChangesAsync();
        await svc.GrantPermissionAsync(role.Id, "Orders.View", "t1");
        cache.Invalidated.Clear();

        await svc.RevokePermissionAsync(role.Id, "Orders.View", "t1");

        (await repo.ListGrantsAsync(PermissionProviderNames.Role, role.Id.ToString(), "t1"))
            .Should().NotContain("Orders.View");
        cache.Invalidated.Should().Contain(PermissionCacheKeys.Role(role.Id, "t1"));
    }

    [Fact]
    public async Task ListGrantsAsync_ReturnsOrderedGrants()
    {
        var (svc, host, _, _) = await BuildAsync();
        var role = new NacRole("R1", tenantId: "t1", isTemplate: false);
        host.Db.Roles.Add(role);
        await host.Db.SaveChangesAsync();
        await svc.GrantPermissionAsync(role.Id, "Orders.View", "t1");
        await svc.GrantPermissionAsync(role.Id, "Orders.Edit", "t1");

        var grants = await svc.ListGrantsAsync(role.Id, "t1");

        grants.Should().ContainInOrder("Orders.Edit", "Orders.View");
    }

    [Fact]
    public async Task DeleteAsync_TemplateRole_Throws()
    {
        var (svc, host, _, _) = await BuildAsync();
        var template = new NacRole("Admin", tenantId: null, isTemplate: true);
        host.Db.Roles.Add(template);
        await host.Db.SaveChangesAsync();

        var act = async () => await svc.DeleteAsync(template.Id);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
