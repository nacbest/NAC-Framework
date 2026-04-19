namespace Nac.Cqrs.Commands;

/// <summary>
/// Marker interface for a command that returns a <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of result produced by this command.</typeparam>
public interface ICommand<TResponse> : IBaseRequest<TResponse>;

/// <summary>
/// Marker interface for a command that produces no result (returns <see cref="Unit"/>).
/// </summary>
public interface ICommand : ICommand<Unit>;
