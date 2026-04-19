using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Context;
using Nac.Identity.IntegrationTests.Infrastructure;
using Nac.Identity.Management.Contracts;
using Nac.Identity.Management.Onboarding;
using Nac.Identity.Memberships;
using Nac.Identity.Permissions;
using Nac.Identity.Permissions.Host;
using Nac.Identity.Roles;
using Nac.Identity.RoleTemplates;
using Nac.Identity.Users;
using Xunit;

namespace Nac.Identity.IntegrationTests.Onboarding;

/// <summary>
/// Exercises <see cref="TenantOnboardingService"/> against a real Postgres database:
/// role templates are cloned idempotently, and host creators never become owners.
/// </summary>
[Collection("Integration")]
public sealed class TenantOnboardingServiceTests : IAsyncLifetime
{
    private readonly PostgresFixture _fx;
    private IdentityIntegrationHost? _host;

    public TenantOnboardingServiceTests(PostgresFixture fx) { _fx = fx; }

    // Seed Identity.Management permission names so template seeder doesn't skip them.
    private sealed class ManagementPerms : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext c)
        {
            var root = c.AddGroup("Identity.Management");
            foreach (var name in new[]
            {
                "Identity.Management.Users.View", "Identity.Management.Users.Manage",
                "Identity.Management.Memberships.View", "Identity.Management.Memberships.Manage",
                "Identity.Management.Roles.View", "Identity.Management.Roles.Manage",
                "Identity.Management.Permissions.Manage", "Identity.Management.Tenants.Manage",
            })
                root.AddPermission(name);
        }
    }

    public async ValueTask InitializeAsync()
    {
        var cs = await _fx.CreateDatabaseAsync($"onb_{Guid.NewGuid():N}");
        _host = await IdentityIntegrationHost.CreateAsync(cs,
            permissionProviders: [new ManagementPerms()],
            roleTemplateProviders: [new DefaultRoleTemplateProvider()]);

        var seeder = new RoleTemplateSeeder(
            _host.GetRequiredService<IServiceScopeFactory>(),
            _host.GetRequiredService<RoleTemplateDefinitionManager>(),
            NullLogger<RoleTemplateSeeder>.Instance);
        await seeder.StartAsync(CancellationToken.None);
    }

    public async ValueTask DisposeAsync() { if (_host is not null) await _host.DisposeAsync(); }

    private TenantOnboardingService BuildService() => new(
        _host!.GetRequiredService<IRoleService>(),
        _host.GetRequiredService<IMembershipService>(),
        _host.GetRequiredService<RoleTemplateDefinitionManager>(),
        _host.GetRequiredService<NacIdentityDbContext>(),
        NullLogger<TenantOnboardingService>.Instance);

    [Fact]
    public async Task OnboardAsync_SeedsThreeRoles_AndCreatesOwnerMembership()
    {
        var creator = new NacUser("owner@example.com", "Owner") { IsHost = false };
        _host!.Db.Users.Add(creator);
        await _host.Db.SaveChangesAsync();

        var svc = BuildService();
        var result = await svc.OnboardAsync("tenant-x", creator.Id);

        result.Status.Should().Be(OnboardingStatus.Seeded);
        result.RoleIds.Should().HaveCount(3);
        result.OwnerMembershipId.Should().NotBeNull();

        var membership = await _host.Db.Memberships.Include(m => m.Roles)
            .FirstAsync(m => m.Id == result.OwnerMembershipId);
        membership.UserId.Should().Be(creator.Id);
        membership.TenantId.Should().Be("tenant-x");
        membership.Status.Should().Be(MembershipStatus.Active);
        membership.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task OnboardAsync_IsIdempotent_WhenInvokedTwice()
    {
        var creator = new NacUser("owner2@example.com");
        _host!.Db.Users.Add(creator);
        await _host.Db.SaveChangesAsync();

        var svc = BuildService();
        var first = await svc.OnboardAsync("tenant-y", creator.Id);
        var second = await svc.OnboardAsync("tenant-y", creator.Id);

        first.Status.Should().Be(OnboardingStatus.Seeded);
        second.Status.Should().Be(OnboardingStatus.AlreadyOnboarded);

        // Roles cloned exactly once.
        var tenantRoles = await _host.Db.Roles.CountAsync(r => r.TenantId == "tenant-y");
        tenantRoles.Should().Be(3);
    }

    [Fact]
    public async Task OnboardAsync_HostCreator_SeedsRolesWithoutMembership()
    {
        var host = new NacUser("host@platform.local") { IsHost = true };
        _host!.Db.Users.Add(host);
        await _host.Db.SaveChangesAsync();

        var svc = BuildService();
        var result = await svc.OnboardAsync("tenant-z", host.Id);

        result.Status.Should().Be(OnboardingStatus.Seeded);
        result.OwnerMembershipId.Should().BeNull("host users must not become tenant owners");
        (await _host.Db.Memberships.CountAsync(m => m.TenantId == "tenant-z")).Should().Be(0);
    }
}
