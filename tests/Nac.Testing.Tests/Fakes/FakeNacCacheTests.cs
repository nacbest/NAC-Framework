using FluentAssertions;
using Nac.Testing.Fakes;
using Xunit;

namespace Nac.Testing.Tests.Fakes;

public class FakeNacCacheTests
{
    [Fact]
    public async Task SetAsync_GetOrCreate_ReturnsValue()
    {
        var cache = new FakeNacCache();
        await cache.SetAsync("key1", "stored");

        var result = await cache.GetOrCreateAsync("key1",
            _ => new ValueTask<string>("from-factory"));

        result.Should().Be("stored");
    }

    [Fact]
    public async Task GetOrCreate_Miss_CallsFactory()
    {
        var cache = new FakeNacCache();
        var factoryCalled = false;

        var result = await cache.GetOrCreateAsync("missing-key", _ =>
        {
            factoryCalled = true;
            return new ValueTask<string>("factory-value");
        });

        factoryCalled.Should().BeTrue();
        result.Should().Be("factory-value");
    }

    [Fact]
    public async Task GetOrCreate_Hit_SkipsFactory()
    {
        var cache = new FakeNacCache();
        // Prime the cache via first call
        await cache.GetOrCreateAsync("hot-key", _ => new ValueTask<string>("original"));

        var factoryCalled = false;
        var result = await cache.GetOrCreateAsync("hot-key", _ =>
        {
            factoryCalled = true;
            return new ValueTask<string>("should-not-be-returned");
        });

        factoryCalled.Should().BeFalse();
        result.Should().Be("original");
    }

    [Fact]
    public async Task Remove_ClearsEntry()
    {
        var cache = new FakeNacCache();
        await cache.SetAsync("del-key", 42);

        await cache.RemoveAsync("del-key");

        cache.Removes.Should().Contain("del-key");
        // After removal the factory should be invoked again
        var factoryCalled = false;
        await cache.GetOrCreateAsync("del-key", _ =>
        {
            factoryCalled = true;
            return new ValueTask<int>(99);
        });
        factoryCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveByTag_TracksTagRemoval()
    {
        var cache = new FakeNacCache();

        await cache.RemoveByTagAsync("my-tag");

        cache.TagRemovals.Should().Contain("my-tag");
    }
}
