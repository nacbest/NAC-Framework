using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.IntegrationTests.Infrastructure;
using Nac.Identity.Permissions;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.RoleTemplates;
using Xunit;

namespace Nac.Identity.IntegrationTests.RoleTemplates;

/// <summary>
/// Verifies the seeder runs idempotently against a real Postgres database and
/// uses stable template ids derived from the template key.
/// </summary>
[Collection("Integration")]
public sealed class RoleTemplateSeederIntegrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _fx;
    private IdentityIntegrationHost? _host;

    public RoleTemplateSeederIntegrationTests(PostgresFixture fx) { _fx = fx; }

    private sealed class Perms : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext c)
        {
            var g = c.AddGroup("Orders");
            g.AddPermission("Orders.View");
            g.AddPermission("Orders.Edit");
        }
    }

    private sealed class TemplateProvider(Action<IRoleTemplateContext> define) : IRoleTemplateProvider
    {
        public void Define(IRoleTemplateContext context) => define(context);
    }

    public async ValueTask InitializeAsync()
    {
        var cs = await _fx.CreateDatabaseAsync($"tpl_{Guid.NewGuid():N}");
        _host = await IdentityIntegrationHost.CreateAsync(cs,
            permissionProviders: [new Perms()],
            roleTemplateProviders: [new TemplateProvider(ctx =>
            {
                ctx.AddTemplate("owner", "Owner").Grants("Orders.View", "Orders.Edit");
                ctx.AddTemplate("viewer", "Viewer").Grants("Orders.View");
            })]);
    }

    public async ValueTask DisposeAsync() { if (_host is not null) await _host.DisposeAsync(); }

    [Fact]
    public async Task StartAsync_SeedsTemplateRoles_WithStableIds()
    {
        var seeder = BuildSeeder();

        await seeder.StartAsync(CancellationToken.None);

        var ownerId = RoleTemplateKeyHasher.ToGuid("owner");
        var viewerId = RoleTemplateKeyHasher.ToGuid("viewer");
        (await _host!.Db.Roles.FirstOrDefaultAsync(r => r.Id == ownerId)).Should().NotBeNull();
        (await _host.Db.Roles.FirstOrDefaultAsync(r => r.Id == viewerId)).Should().NotBeNull();

        var ownerGrants = await _host.Db.PermissionGrants
            .Where(g => g.ProviderName == PermissionProviderNames.Role && g.ProviderKey == ownerId.ToString())
            .Select(g => g.PermissionName).ToListAsync();
        ownerGrants.Should().BeEquivalentTo(["Orders.View", "Orders.Edit"]);
    }

    [Fact]
    public async Task StartAsync_RunsMultipleTimes_WithoutDuplicates()
    {
        var seeder = BuildSeeder();
        await seeder.StartAsync(CancellationToken.None);
        await seeder.StartAsync(CancellationToken.None);

        (await _host!.Db.Roles.CountAsync(r => r.IsTemplate)).Should().Be(2);
        (await _host.Db.PermissionGrants.CountAsync()).Should().Be(3);
    }

    private RoleTemplateSeeder BuildSeeder() => new(
        _host!.GetRequiredService<IServiceScopeFactory>(),
        _host.GetRequiredService<RoleTemplateDefinitionManager>(),
        NullLogger<RoleTemplateSeeder>.Instance);
}
