using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Nac.Observability.Logging;
using Xunit;

namespace Nac.Observability.Tests.Logging;

public class NacLoggingScopeTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Fact]
    public void BeginNacScope_WithAllValues_CallsBeginScope()
    {
        // Arrange
        _logger.BeginScope(Arg.Any<Dictionary<string, object?>>())
            .Returns(Substitute.For<IDisposable>());

        // Act
        var scope = _logger.BeginNacScope(tenantId: "t1", userId: "u1", correlationId: "c1");

        // Assert
        scope.Should().NotBeNull();
        _logger.Received(1).BeginScope(Arg.Is<Dictionary<string, object?>>(d =>
            d.ContainsKey("TenantId") && d.ContainsKey("UserId") && d.ContainsKey("CorrelationId")));
    }

    [Fact]
    public void BeginNacScope_WithTenantIdOnly_IncludesTenantId()
    {
        // Arrange
        _logger.BeginScope(Arg.Any<Dictionary<string, object?>>())
            .Returns(Substitute.For<IDisposable>());

        // Act
        _logger.BeginNacScope(tenantId: "t1");

        // Assert
        _logger.Received(1).BeginScope(Arg.Is<Dictionary<string, object?>>(d =>
            d.ContainsKey("TenantId") && !d.ContainsKey("UserId") && !d.ContainsKey("CorrelationId")));
    }

    [Fact]
    public void BeginNacScope_WithUserIdOnly_IncludesUserId()
    {
        // Arrange
        _logger.BeginScope(Arg.Any<Dictionary<string, object?>>())
            .Returns(Substitute.For<IDisposable>());

        // Act
        _logger.BeginNacScope(userId: "u1");

        // Assert
        _logger.Received(1).BeginScope(Arg.Is<Dictionary<string, object?>>(d =>
            !d.ContainsKey("TenantId") && d.ContainsKey("UserId")));
    }

    [Fact]
    public void BeginNacScope_WithCorrelationIdOnly_IncludesCorrelationId()
    {
        // Arrange
        _logger.BeginScope(Arg.Any<Dictionary<string, object?>>())
            .Returns(Substitute.For<IDisposable>());

        // Act
        _logger.BeginNacScope(correlationId: "c1");

        // Assert
        _logger.Received(1).BeginScope(Arg.Is<Dictionary<string, object?>>(d =>
            d.ContainsKey("CorrelationId") && d.Count == 1));
    }

    [Fact]
    public void BeginNacScope_WithNoValues_ReturnsNull()
    {
        // Act
        var scope = _logger.BeginNacScope();

        // Assert
        scope.Should().BeNull();
        _logger.DidNotReceive().BeginScope(Arg.Any<Dictionary<string, object?>>());
    }

    [Fact]
    public void BeginNacScope_WithAllNull_ReturnsNull()
    {
        // Act
        var scope = _logger.BeginNacScope(tenantId: null, userId: null, correlationId: null);

        // Assert
        scope.Should().BeNull();
    }

    [Fact]
    public void BeginNacScope_ReturnedScope_IsDisposable()
    {
        // Arrange
        var fakeScope = Substitute.For<IDisposable>();
        _logger.BeginScope(Arg.Any<Dictionary<string, object?>>()).Returns(fakeScope);

        // Act
        var scope = _logger.BeginNacScope(tenantId: "t1");

        // Assert
        scope.Should().BeAssignableTo<IDisposable>();
        scope!.Dispose();
        fakeScope.Received(1).Dispose();
    }
}
