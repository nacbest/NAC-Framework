using Nac.Core.Results;
using Nac.Cqrs.Commands;

namespace Nac.Cqrs.Tests.Helpers;

public sealed record CreateUserCommand(string Name, string Email) : ICommand<Result<string>>;

public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Result<string>>
{
    public ValueTask<Result<string>> HandleAsync(CreateUserCommand command, CancellationToken ct = default)
    {
        return ValueTask.FromResult(Result<string>.Success($"user-{command.Name}"));
    }
}
