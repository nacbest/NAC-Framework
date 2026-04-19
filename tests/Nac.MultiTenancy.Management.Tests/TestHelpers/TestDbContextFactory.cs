using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nac.MultiTenancy.Management.Abstractions;
using Nac.MultiTenancy.Management.Persistence;
using Nac.MultiTenancy.Management.Services;
using Nac.MultiTenancy.Management.Validators;

namespace Nac.MultiTenancy.Management.Tests.TestHelpers;

/// <summary>
/// Spins up an in-memory <see cref="TenantManagementDbContext"/> + service stack
/// for unit tests. Each test gets a fresh database via a unique GUID database name.
/// </summary>
internal static class TestDbContextFactory
{
    public static TenantManagementDbContext CreateDb(string? name = null)
    {
        var opts = new DbContextOptionsBuilder<TenantManagementDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;
        return new TenantManagementDbContext(opts);
    }

    public static (TenantManagementService Service, TenantManagementDbContext Db, IMemoryCache Cache, IDataProtectionProvider Dp)
        CreateService(string? name = null)
    {
        var db = CreateDb(name);
        var cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        var invalidator = new TenantCacheInvalidator(cache);
        var dp = new EphemeralDataProtectionProvider();

        var options = Options.Create(new TenantManagementOptions
        {
            DefaultPageSize = 20,
            MaxPageSize = 100,
            MaxBulkSize = 100,
        });

        var service = new TenantManagementService(
            db,
            new CreateTenantRequestValidator(),
            new UpdateTenantRequestValidator(),
            dp,
            invalidator,
            options,
            NullLogger<TenantManagementService>.Instance);

        return (service, db, cache, dp);
    }
}
