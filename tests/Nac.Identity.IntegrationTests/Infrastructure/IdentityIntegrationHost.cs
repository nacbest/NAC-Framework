using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nac.Core.Abstractions.Events;
using Nac.Core.Abstractions.Permissions;
using Nac.EventBus.Abstractions;
using Nac.Identity.Context;
using Nac.Identity.Jwt;
using Nac.Identity.Memberships;
using Nac.Identity.Permissions;
using Nac.Identity.Permissions.Cache;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.Permissions.Host;
using Nac.Identity.Roles;
using Nac.Identity.RoleTemplates;
using Nac.Identity.Services;
using Nac.Identity.Users;

namespace Nac.Identity.IntegrationTests.Infrastructure;

/// <summary>
/// DI container wired against a real Postgres <see cref="TestIdentityDbContext"/>.
/// Mirrors the minimum subset of <c>AddNacIdentity&lt;T&gt;</c> needed for
/// service-level integration tests (no HTTP auth / JWT validation middleware).
/// </summary>
public sealed class IdentityIntegrationHost : IAsyncDisposable
{
    public ServiceProvider Services { get; }
    public TestIdentityDbContext Db => Services.GetRequiredService<TestIdentityDbContext>();

    private IdentityIntegrationHost(ServiceProvider sp) { Services = sp; }

    public static async Task<IdentityIntegrationHost> CreateAsync(string connectionString,
        IEnumerable<IPermissionDefinitionProvider>? permissionProviders = null,
        IEnumerable<IRoleTemplateProvider>? roleTemplateProviders = null,
        IEventPublisher? eventPublisher = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<TestIdentityDbContext>(opt => opt.UseNpgsql(connectionString));
        services.AddScoped<NacIdentityDbContext>(sp => sp.GetRequiredService<TestIdentityDbContext>());

        services.AddIdentityCore<NacUser>()
                .AddRoles<NacRole>()
                .AddEntityFrameworkStores<TestIdentityDbContext>();

        services.Configure<JwtOptions>(o =>
        {
            o.SecretKey = "integration-test-secret-key-at-least-32-chars-long!";
            o.Issuer = "IntTest";
            o.Audience = "IntTest";
            o.ExpirationMinutes = 60;
        });

        services.AddHttpContextAccessor();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<ITenantSwitchService, TenantSwitchService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IRoleService, RoleService>();

        services.AddDistributedMemoryCache();

        // Permission definition providers
        services.AddSingleton<IPermissionDefinitionProvider, HostPermissionProvider>();
        foreach (var p in permissionProviders ?? []) services.AddSingleton(p);
        services.AddSingleton<PermissionDefinitionManager>();

        services.AddSingleton<IPermissionGrantCache, DistributedPermissionGrantCache>();
        services.AddScoped<IPermissionGrantRepository, EfCorePermissionGrantRepository>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();

        // Role templates
        foreach (var tp in roleTemplateProviders ?? []) services.AddSingleton(tp);
        services.AddSingleton<RoleTemplateDefinitionManager>();

        // Event publisher — stub by default; integration tests may supply a real one.
        if (eventPublisher is not null)
            services.AddSingleton(eventPublisher);
        else
            services.AddSingleton<IEventPublisher, NoOpEventPublisher>();

        var sp = services.BuildServiceProvider();

        // Create schema — EnsureCreated produces a clean model-driven schema; we don't
        // exercise migrations here.
        await using (var scope = sp.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestIdentityDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return new IdentityIntegrationHost(sp);
    }

    public T GetRequiredService<T>() where T : notnull => Services.GetRequiredService<T>();

    public async ValueTask DisposeAsync() => await Services.DisposeAsync();

    private sealed class NoOpEventPublisher : IEventPublisher
    {
        public Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default) => Task.CompletedTask;
        public Task PublishAsync(IEnumerable<IIntegrationEvent> events, CancellationToken ct = default) => Task.CompletedTask;
    }
}
