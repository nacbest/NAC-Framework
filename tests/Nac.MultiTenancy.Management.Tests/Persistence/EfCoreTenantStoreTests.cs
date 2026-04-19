using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Nac.MultiTenancy.Management.Abstractions;
using Nac.MultiTenancy.Management.Domain;
using Nac.MultiTenancy.Management.Persistence;
using Nac.MultiTenancy.Management.Tests.TestHelpers;
using Xunit;

namespace Nac.MultiTenancy.Management.Tests.Persistence;

public class EfCoreTenantStoreTests
{
    private static IMemoryCache NewCache() => new MemoryCache(Options.Create(new MemoryCacheOptions()));

    [Fact]
    public async Task GetByIdAsync_Active_ReturnsInfo()
    {
        var db = TestDbContextFactory.CreateDb();
        db.Tenants.Add(Tenant.Create(Guid.NewGuid(), "acme", "Acme", TenantIsolationMode.Shared, null, null));
        await db.SaveChangesAsync();
        var store = new EfCoreTenantStore(db, NewCache());

        var info = await store.GetByIdAsync("acme");

        info.Should().NotBeNull();
        info!.Id.Should().Be("acme");
        info.Name.Should().Be("Acme");
    }

    [Fact]
    public async Task GetByIdAsync_Deactivated_ReturnsNull()
    {
        var db = TestDbContextFactory.CreateDb();
        var t = Tenant.Create(Guid.NewGuid(), "acme", "Acme", TenantIsolationMode.Shared, null, null);
        t.Deactivate();
        db.Tenants.Add(t);
        await db.SaveChangesAsync();
        var store = new EfCoreTenantStore(db, NewCache());

        var info = await store.GetByIdAsync("acme");

        info.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_SoftDeleted_ReturnsNull()
    {
        var db = TestDbContextFactory.CreateDb();
        var t = Tenant.Create(Guid.NewGuid(), "acme", "Acme", TenantIsolationMode.Shared, null, null);
        t.MarkDeleted();
        db.Tenants.Add(t);
        await db.SaveChangesAsync();
        var store = new EfCoreTenantStore(db, NewCache());

        var info = await store.GetByIdAsync("acme");

        info.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_SecondCall_HitsCache()
    {
        var db = TestDbContextFactory.CreateDb();
        db.Tenants.Add(Tenant.Create(Guid.NewGuid(), "acme", "Acme", TenantIsolationMode.Shared, null, null));
        await db.SaveChangesAsync();
        var cache = NewCache();
        var store = new EfCoreTenantStore(db, cache);

        await store.GetByIdAsync("acme");
        cache.TryGetValue(TenantCacheInvalidator.IdentifierKeyPrefix + "acme", out _).Should().BeTrue();
    }
}
