using FluentAssertions;
using Nac.Core.Abstractions;
using Nac.Cqrs.Commands;
using Nac.Cqrs.Markers;
using Nac.Cqrs.Pipeline;
using NSubstitute;
using Xunit;

namespace Nac.Cqrs.Tests.Pipeline;

public class TransactionBehaviorTests
{
    [Fact]
    public async Task HandleAsync_NonTransactional_CallsNextWithoutSave()
    {
        // Arrange
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var behavior = new TransactionBehavior<TestNonTransactionalCommand, string>(unitOfWork);
        var command = new TestNonTransactionalCommand("test");
        var nextResult = "next result";
        var nextCalled = false;

        RequestHandlerDelegate<string> next = async () =>
        {
            nextCalled = true;
            return await ValueTask.FromResult(nextResult).ConfigureAwait(false);
        };

        // Act
        var result = await behavior.HandleAsync(command, next);

        // Assert
        result.Should().Be(nextResult);
        nextCalled.Should().BeTrue();
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Transactional_SavesAfterHandler()
    {
        // Arrange
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var saveWasCalled = false;
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                saveWasCalled = true;
                return Task.FromResult(1);
            });

        var behavior = new TransactionBehavior<TestTransactionalCommand, string>(unitOfWork);
        var command = new TestTransactionalCommand("test");

        RequestHandlerDelegate<string> next = async () =>
            await ValueTask.FromResult("result").ConfigureAwait(false);

        // Act
        var result = await behavior.HandleAsync(command, next);

        // Assert
        result.Should().Be("result");
        saveWasCalled.Should().BeTrue();
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Transactional_OnException_DoesNotSave()
    {
        // Arrange
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var behavior = new TransactionBehavior<TestTransactionalCommand, string>(unitOfWork);
        var command = new TestTransactionalCommand("test");

        RequestHandlerDelegate<string> next = async () =>
        {
            await ValueTask.CompletedTask;
            throw new InvalidOperationException("Handler failed");
        };

        // Act
        var act = async () => await behavior.HandleAsync(command, next);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Transactional_PassesCancellationToken()
    {
        // Arrange
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var behavior = new TransactionBehavior<TestTransactionalCommand, string>(unitOfWork);
        var command = new TestTransactionalCommand("test");
        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        RequestHandlerDelegate<string> next = async () =>
            await ValueTask.FromResult("result").ConfigureAwait(false);

        // Act
        var result = await behavior.HandleAsync(command, next, ct);

        // Assert
        result.Should().Be("result");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Is<CancellationToken>(t => t == ct));
    }

    // Test helpers
    private sealed record TestNonTransactionalCommand(string Value) : ICommand<string>;

    private sealed record TestTransactionalCommand(string Value) : ICommand<string>, ITransactionalCommand;
}
