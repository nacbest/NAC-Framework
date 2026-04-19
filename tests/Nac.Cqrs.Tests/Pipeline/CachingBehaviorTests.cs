using FluentAssertions;
using Nac.Caching;
using Nac.Cqrs.Markers;
using Nac.Cqrs.Pipeline;
using Nac.Cqrs.Queries;
using NSubstitute;
using Xunit;

namespace Nac.Cqrs.Tests.Pipeline;

public class CachingBehaviorTests
{
    [Fact]
    public async Task HandleAsync_NonCacheableRequest_CallsNext()
    {
        // Arrange
        var cache = Substitute.For<INacCache>();
        var behavior = new CachingBehavior<TestNonCacheableQuery, string>(cache);
        var query = new TestNonCacheableQuery("test");
        var nextResult = "next result";
        var nextCalled = false;

        RequestHandlerDelegate<string> next = async () =>
        {
            nextCalled = true;
            return await ValueTask.FromResult(nextResult).ConfigureAwait(false);
        };

        // Act
        var result = await behavior.HandleAsync(query, next);

        // Assert
        result.Should().Be(nextResult);
        nextCalled.Should().BeTrue();
        await cache.DidNotReceive().GetOrCreateAsync<string>(
            Arg.Any<string>(),
            Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
            Arg.Any<CacheEntryOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CacheableQuery_DelegatesToCache()
    {
        // Arrange
        var cache = Substitute.For<INacCache>();
        var cachedValue = "cached-result";
        cache.GetOrCreateAsync<string>(
            Arg.Any<string>(),
            Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
            Arg.Any<CacheEntryOptions?>(),
            Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(cachedValue));

        var behavior = new CachingBehavior<TestCacheableQuery, string>(cache);
        var query = new TestCacheableQuery("test");

        RequestHandlerDelegate<string> next = async () =>
            await ValueTask.FromResult("not-used").ConfigureAwait(false);

        // Act
        var result = await behavior.HandleAsync(query, next);

        // Assert
        result.Should().Be(cachedValue);
        await cache.Received(1).GetOrCreateAsync<string>(
            Arg.Is<string>(k => k == "query:test"),
            Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
            Arg.Any<CacheEntryOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CacheableQuery_WithDuration_PassesDuration()
    {
        // Arrange
        var cache = Substitute.For<INacCache>();
        var cachedValue = "cached-result";
        var capturedOptions = (CacheEntryOptions?)null;

        cache.GetOrCreateAsync<string>(
            Arg.Any<string>(),
            Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
            Arg.Any<CacheEntryOptions?>(),
            Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedOptions = x.ArgAt<CacheEntryOptions?>(2);
                return ValueTask.FromResult(cachedValue);
            });

        var behavior = new CachingBehavior<TestCacheableQueryWithDuration, string>(cache);
        var query = new TestCacheableQueryWithDuration("test");

        RequestHandlerDelegate<string> next = async () =>
            await ValueTask.FromResult("not-used").ConfigureAwait(false);

        // Act
        var result = await behavior.HandleAsync(query, next);

        // Assert
        result.Should().Be(cachedValue);
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Expiration.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public async Task HandleAsync_CacheableQuery_WithTags_PassesTags()
    {
        // Arrange
        var cache = Substitute.For<INacCache>();
        var cachedValue = "cached-result";
        var capturedOptions = (CacheEntryOptions?)null;

        cache.GetOrCreateAsync<string>(
            Arg.Any<string>(),
            Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
            Arg.Any<CacheEntryOptions?>(),
            Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedOptions = x.ArgAt<CacheEntryOptions?>(2);
                return ValueTask.FromResult(cachedValue);
            });

        var behavior = new CachingBehavior<TestCacheableQueryWithTags, string>(cache);
        var query = new TestCacheableQueryWithTags("test");

        RequestHandlerDelegate<string> next = async () =>
            await ValueTask.FromResult("not-used").ConfigureAwait(false);

        // Act
        var result = await behavior.HandleAsync(query, next);

        // Assert
        result.Should().Be(cachedValue);
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Tags.Should().Equal("tag1", "tag2");
    }

    [Fact]
    public async Task HandleAsync_CacheableQuery_NoDurationNoTags_PassesNullOptions()
    {
        // Arrange
        var cache = Substitute.For<INacCache>();
        var cachedValue = "cached-result";
        var capturedOptions = (CacheEntryOptions?)new CacheEntryOptions(); // non-null placeholder

        cache.GetOrCreateAsync<string>(
            Arg.Any<string>(),
            Arg.Any<Func<CancellationToken, ValueTask<string>>>(),
            Arg.Any<CacheEntryOptions?>(),
            Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                capturedOptions = x.ArgAt<CacheEntryOptions?>(2);
                return ValueTask.FromResult(cachedValue);
            });

        var behavior = new CachingBehavior<TestCacheableQuery, string>(cache);
        var query = new TestCacheableQuery("test");

        RequestHandlerDelegate<string> next = async () =>
            await ValueTask.FromResult("not-used").ConfigureAwait(false);

        // Act
        var result = await behavior.HandleAsync(query, next);

        // Assert
        result.Should().Be(cachedValue);
        capturedOptions.Should().BeNull();
    }

    // Test helpers
    private sealed record TestNonCacheableQuery(string Value) : IQuery<string>;

    private sealed record TestCacheableQuery(string Value) : IQuery<string>, ICacheableQuery
    {
        public string CacheKey => $"query:{Value}";
    }

    private sealed record TestCacheableQueryWithDuration(string Value) : IQuery<string>, ICacheableQuery
    {
        public string CacheKey => $"query:{Value}";
        public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    }

    private sealed record TestCacheableQueryWithTags(string Value) : IQuery<string>, ICacheableQuery
    {
        public string CacheKey => $"query:{Value}";
        public IReadOnlyList<string> CacheTags => new[] { "tag1", "tag2" };
    }
}
