using Nac.Cqrs.Commands;

namespace Nac.Cqrs.Tests.Helpers;

public sealed record TestCommand(string Name) : ICommand<string>;

public sealed class TestCommandHandler : ICommandHandler<TestCommand, string>
{
    public ValueTask<string> HandleAsync(TestCommand command, CancellationToken ct = default)
    {
        return ValueTask.FromResult($"Hello, {command.Name}!");
    }
}
