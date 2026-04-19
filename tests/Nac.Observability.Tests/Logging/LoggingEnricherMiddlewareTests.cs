using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nac.Core.Abstractions.Identity;
using Nac.Observability.Logging;
using NSubstitute;
using Xunit;

namespace Nac.Observability.Tests.Logging;

public class LoggingEnricherMiddlewareTests
{
    private readonly ILogger<LoggingEnricherMiddleware> _logger =
        Substitute.For<ILogger<LoggingEnricherMiddleware>>();

    private HttpContext CreateHttpContext(ICurrentUser? currentUser = null)
    {
        var services = new ServiceCollection();
        if (currentUser is not null)
            services.AddSingleton(currentUser);

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        return context;
    }

    [Fact]
    public async Task InvokeAsync_WithAuthenticatedUser_EnrichesScope()
    {
        // Arrange
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.Id.Returns(Guid.NewGuid());
        user.TenantId.Returns("tenant-1");

        _logger.BeginScope(Arg.Any<Dictionary<string, object?>>())
            .Returns(Substitute.For<IDisposable>());

        var nextCalled = false;
        var middleware = new LoggingEnricherMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; }, _logger);

        // Act
        await middleware.InvokeAsync(CreateHttpContext(user));

        // Assert
        nextCalled.Should().BeTrue();
        _logger.Received(1).BeginScope(Arg.Is<Dictionary<string, object?>>(d =>
            d.ContainsKey("TenantId") && d.ContainsKey("UserId")));
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthenticatedUser_NoEnrichment()
    {
        // Arrange
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(false);

        var middleware = new LoggingEnricherMiddleware(
            _ => Task.CompletedTask, _logger);

        // Act
        await middleware.InvokeAsync(CreateHttpContext(user));

        // Assert — BeginNacScope returns null when no values, so BeginScope not called
        _logger.DidNotReceive().BeginScope(Arg.Any<Dictionary<string, object?>>());
    }

    [Fact]
    public async Task InvokeAsync_WithNullCurrentUser_NoEnrichment()
    {
        // Arrange
        var middleware = new LoggingEnricherMiddleware(
            _ => Task.CompletedTask, _logger);

        // Act
        await middleware.InvokeAsync(CreateHttpContext());

        // Assert
        _logger.DidNotReceive().BeginScope(Arg.Any<Dictionary<string, object?>>());
    }

    [Fact]
    public async Task InvokeAsync_AlwaysCallsNext()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new LoggingEnricherMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; }, _logger);

        // Act
        await middleware.InvokeAsync(CreateHttpContext());

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithTenantId_IncludesTenantInScope()
    {
        // Arrange
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.Id.Returns(Guid.NewGuid());
        user.TenantId.Returns("my-tenant");

        _logger.BeginScope(Arg.Any<Dictionary<string, object?>>())
            .Returns(Substitute.For<IDisposable>());

        var middleware = new LoggingEnricherMiddleware(
            _ => Task.CompletedTask, _logger);

        // Act
        await middleware.InvokeAsync(CreateHttpContext(user));

        // Assert
        _logger.Received(1).BeginScope(Arg.Is<Dictionary<string, object?>>(d =>
            (string?)d["TenantId"] == "my-tenant"));
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_ScopeStillDisposed()
    {
        // Arrange
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(true);
        user.Id.Returns(Guid.NewGuid());
        user.TenantId.Returns("t1");

        var fakeScope = Substitute.For<IDisposable>();
        _logger.BeginScope(Arg.Any<Dictionary<string, object?>>()).Returns(fakeScope);

        var middleware = new LoggingEnricherMiddleware(
            _ => throw new InvalidOperationException("boom"), _logger);

        // Act
        var act = () => middleware.InvokeAsync(CreateHttpContext(user));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        fakeScope.Received(1).Dispose();
    }

    [Fact]
    public void Constructor_WithValidArgs_DoesNotThrow()
    {
        // Act
        var act = () => new LoggingEnricherMiddleware(
            _ => Task.CompletedTask, _logger);

        // Assert
        act.Should().NotThrow();
    }
}
