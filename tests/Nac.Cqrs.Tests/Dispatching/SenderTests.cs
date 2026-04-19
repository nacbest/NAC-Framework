using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nac.Cqrs.Commands;
using Nac.Cqrs.Dispatching;
using Nac.Cqrs.Extensions;
using Nac.Cqrs.Pipeline;
using Nac.Cqrs.Tests.Helpers;
using NSubstitute;
using Xunit;

namespace Nac.Cqrs.Tests.Dispatching;

public class SenderTests
{
    [Fact]
    public async Task SendAsync_Command_RoutesToCommandHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNacCqrs(opts =>
            opts.RegisterHandlersFromAssembly(typeof(TestCommandHandler).Assembly));
        var sp = services.BuildServiceProvider();

        var sender = sp.GetRequiredService<ISender>();
        var command = new TestCommand("Alice");

        // Act
        var response = await sender.SendAsync(command);

        // Assert
        response.Should().Be("Hello, Alice!");
    }

    [Fact]
    public async Task SendAsync_Query_RoutesToQueryHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNacCqrs(opts =>
            opts.RegisterHandlersFromAssembly(typeof(TestQueryHandler).Assembly));
        var sp = services.BuildServiceProvider();

        var sender = sp.GetRequiredService<ISender>();
        var query = new TestQuery(42);

        // Act
        var response = await sender.SendAsync(query);

        // Assert
        response.Should().Be("Item-42");
    }

    [Fact]
    public async Task SendAsync_UnregisteredHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNacCqrs(opts =>
            opts.RegisterHandlersFromAssembly(typeof(TestCommandHandler).Assembly));
        var sp = services.BuildServiceProvider();

        var sender = sp.GetRequiredService<ISender>();
        var unregisteredCommand = new UnregisteredCommand("test");

        // Act
        var act = async () => await sender.SendAsync(unregisteredCommand);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No handler registered for request type*");
    }

    [Fact]
    public async Task SendAsync_WithPipelineBehavior_ExecutesBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        var behaviorWasCalled = false;

        // Create a real behavior that tracks if it was called
        services.AddScoped(sp =>
            (IPipelineBehavior<TestCommand, string>)new TrackingBehavior(() => behaviorWasCalled = true)
        );

        services.AddNacCqrs(opts =>
            opts.RegisterHandlersFromAssembly(typeof(TestCommandHandler).Assembly));
        var sp2 = services.BuildServiceProvider();

        var sender = sp2.GetRequiredService<ISender>();
        var command = new TestCommand("Bob");

        // Act
        var response = await sender.SendAsync(command);

        // Assert
        response.Should().Be("Hello, Bob!");
        behaviorWasCalled.Should().BeTrue();
    }

    // Helper behavior for testing pipeline execution
    private sealed class TrackingBehavior : IPipelineBehavior<TestCommand, string>
    {
        private readonly Action _onCalled;

        public TrackingBehavior(Action onCalled)
        {
            _onCalled = onCalled;
        }

        public async ValueTask<string> HandleAsync(
            TestCommand request,
            RequestHandlerDelegate<string> next,
            CancellationToken ct = default)
        {
            _onCalled();
            return await next().ConfigureAwait(false);
        }
    }

    [Fact]
    public async Task SendAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNacCqrs(opts =>
            opts.RegisterHandlersFromAssembly(typeof(TestCommandHandler).Assembly));
        var sp = services.BuildServiceProvider();

        var sender = sp.GetRequiredService<ISender>();

        // Act
        var act = async () => await sender.SendAsync<string>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // Helper: unregistered command for testing unknown handler scenario
    private sealed record UnregisteredCommand(string Value) : ICommand<string>;
}
