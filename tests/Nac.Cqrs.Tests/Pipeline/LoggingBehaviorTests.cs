using FluentAssertions;
using Microsoft.Extensions.Logging;
using Nac.Cqrs.Pipeline;
using Nac.Cqrs.Tests.Helpers;
using Xunit;

namespace Nac.Cqrs.Tests.Pipeline;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task HandleAsync_LogsRequestTypeOnEntry()
    {
        // Arrange - create a mock logger that captures calls
        var loggedMessages = new List<string>();
        var mockLogger = new MockLogger(loggedMessages);
        var behavior = new LoggingBehavior<TestCommand, string>(mockLogger);
        var command = new TestCommand("Test");

        RequestHandlerDelegate<string> next = async () =>
            await ValueTask.FromResult("result").ConfigureAwait(false);

        // Act
        await behavior.HandleAsync(command, next);

        // Assert
        // Should have logged the entry message
        loggedMessages.Should().Contain(msg => msg.Contains("Handling"));
    }

    [Fact]
    public async Task HandleAsync_NormalSpeed_LogsDebugCompletion()
    {
        // Arrange
        var loggedMessages = new List<string>();
        var mockLogger = new MockLogger(loggedMessages);
        var behavior = new LoggingBehavior<TestCommand, string>(mockLogger);
        var command = new TestCommand("Test");

        RequestHandlerDelegate<string> next = async () =>
        {
            // Simulate a fast operation (< 500ms)
            await Task.Delay(10);
            return "result";
        };

        // Act
        await behavior.HandleAsync(command, next);

        // Assert
        // Should have logged entry and completion
        loggedMessages.Should().HaveCountGreaterThanOrEqualTo(2);
        // Should NOT have warning (no slow request)
        loggedMessages.Should().NotContain(msg => msg.Contains("Slow"));
    }

    [Fact]
    public async Task HandleAsync_SlowRequest_LogsWarning()
    {
        // Arrange
        var loggedMessages = new List<string>();
        var mockLogger = new MockLogger(loggedMessages);
        var behavior = new LoggingBehavior<TestCommand, string>(mockLogger);
        var command = new TestCommand("Test");

        RequestHandlerDelegate<string> next = async () =>
        {
            // Simulate a slow operation (> 500ms)
            await Task.Delay(600);
            return "result";
        };

        // Act
        await behavior.HandleAsync(command, next);

        // Assert
        // Should have logged a Warning for slow request
        loggedMessages.Should().Contain(msg => msg.Contains("Slow"));
    }

    [Fact]
    public async Task HandleAsync_PassesThroughReturnValue()
    {
        // Arrange
        var loggedMessages = new List<string>();
        var mockLogger = new MockLogger(loggedMessages);
        var behavior = new LoggingBehavior<TestCommand, string>(mockLogger);
        var command = new TestCommand("Test");
        var expectedResult = "expected-result";

        RequestHandlerDelegate<string> next = async () =>
            await ValueTask.FromResult(expectedResult).ConfigureAwait(false);

        // Act
        var result = await behavior.HandleAsync(command, next);

        // Assert
        result.Should().Be(expectedResult);
    }

    // Mock logger implementation
    private sealed class MockLogger : ILogger<LoggingBehavior<TestCommand, string>>
    {
        private readonly List<string> _messages;

        public MockLogger(List<string> messages)
        {
            _messages = messages;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _messages.Add(formatter(state, exception));
        }
    }
}
