using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Context;
using Nac.Identity.Permissions;
using Nac.Identity.Users;

namespace Nac.Identity.Tests.Infrastructure;

/// <summary>
/// Builds a DI container wired with the minimal Identity + permission surface required
/// by service-level unit tests (InMemory EF, no HTTP stack). Disposable.
/// </summary>
public sealed class IdentityTestHost : IAsyncDisposable
{
    public ServiceProvider Services { get; }
    public TestIdentityDbContext Db => Services.GetRequiredService<TestIdentityDbContext>();

    private IdentityTestHost(ServiceProvider sp) { Services = sp; }

    public static IdentityTestHost Create(
        Action<IServiceCollection>? configure = null,
        IEnumerable<IPermissionDefinitionProvider>? permissionProviders = null)
    {
        var services = new ServiceCollection();
        var dbName = $"test-{Guid.NewGuid():N}";

        services.AddDbContext<TestIdentityDbContext>(opt => opt
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddScoped<NacIdentityDbContext>(sp => sp.GetRequiredService<TestIdentityDbContext>());

        services.AddLogging();
        services.AddIdentityCore<NacUser>()
                .AddRoles<NacRole>()
                .AddEntityFrameworkStores<TestIdentityDbContext>();

        var providers = permissionProviders?.ToList() ?? [];
        foreach (var p in providers) services.AddSingleton(p);
        services.AddSingleton(sp => new PermissionDefinitionManager(
            sp.GetServices<IPermissionDefinitionProvider>()));

        configure?.Invoke(services);

        var sp = services.BuildServiceProvider();
        return new IdentityTestHost(sp);
    }

    public T GetRequiredService<T>() where T : notnull => Services.GetRequiredService<T>();

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
    }
}
