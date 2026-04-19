using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Nac.Core.Abstractions.Identity;
using NSubstitute;
using Xunit;

namespace Nac.Caching.Tests;

public class NacCacheTests
{
    private readonly HybridCache _hybridCache = Substitute.For<HybridCache>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IOptions<NacCacheOptions> _options = Options.Create(
        new NacCacheOptions { DefaultExpiration = TimeSpan.FromMinutes(5) });

    [Fact]
    public async Task GetOrCreateAsync_NoTenant_UsesPlainKey()
    {
        // Arrange
        _serviceProvider
            .GetService(typeof(ICurrentUser))
            .Returns(null);

        var cache = new NacCache(_hybridCache, _serviceProvider, _options);
        var key = "key";
        var testValue = "test-value";
        var factory = (CancellationToken _) => new ValueTask<string>(testValue);

        _hybridCache
            .GetOrCreateAsync<string>(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
                Arg.Any<HybridCacheEntryOptions>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<string>(testValue));

        // Act
        var result = await cache.GetOrCreateAsync(key, factory);

        // Assert
        result.Should().Be(testValue);
        await _hybridCache
            .Received(1)
            .GetOrCreateAsync<string>(
                key,
                Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
                Arg.Any<HybridCacheEntryOptions>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrCreateAsync_WithTenant_PrependsPrefix()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.TenantId.Returns("t1");

        _serviceProvider
            .GetService(typeof(ICurrentUser))
            .Returns(currentUser);

        var cache = new NacCache(_hybridCache, _serviceProvider, _options);
        var key = "key";
        var testValue = "test-value";
        var factory = (CancellationToken _) => new ValueTask<string>(testValue);

        _hybridCache
            .GetOrCreateAsync<string>(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
                Arg.Any<HybridCacheEntryOptions>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<string>(testValue));

        // Act
        var result = await cache.GetOrCreateAsync(key, factory);

        // Assert
        result.Should().Be(testValue);
        await _hybridCache
            .Received(1)
            .GetOrCreateAsync<string>(
                "t1:key",
                Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
                Arg.Any<HybridCacheEntryOptions>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_DelegatesToHybridCache()
    {
        // Arrange
        _serviceProvider
            .GetService(typeof(ICurrentUser))
            .Returns(null);

        var cache = new NacCache(_hybridCache, _serviceProvider, _options);
        var key = "key";
        var testValue = "test-value";

        _hybridCache
            .SetAsync<string>(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<HybridCacheEntryOptions>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await cache.SetAsync(key, testValue);

        // Assert
        await _hybridCache
            .Received(1)
            .SetAsync<string>(
                key,
                testValue,
                Arg.Any<HybridCacheEntryOptions>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_BuildsKeyAndDelegates()
    {
        // Arrange
        _serviceProvider
            .GetService(typeof(ICurrentUser))
            .Returns(null);

        var cache = new NacCache(_hybridCache, _serviceProvider, _options);
        var key = "k";

        _hybridCache
            .RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await cache.RemoveAsync(key);

        // Assert
        await _hybridCache
            .Received(1)
            .RemoveAsync(key, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveByTagAsync_PrependsTenantToTag()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.TenantId.Returns("t1");

        _serviceProvider
            .GetService(typeof(ICurrentUser))
            .Returns(currentUser);

        var cache = new NacCache(_hybridCache, _serviceProvider, _options);
        var tag = "tag1";

        _hybridCache
            .RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        // Act
        await cache.RemoveByTagAsync(tag);

        // Assert
        await _hybridCache
            .Received(1)
            .RemoveByTagAsync("t1:tag1", Arg.Any<CancellationToken>());
    }
}
