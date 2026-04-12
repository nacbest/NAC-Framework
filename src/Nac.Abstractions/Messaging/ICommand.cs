namespace Nac.Messaging;

/// <summary>
/// Marker interface for commands that do not return a result.
/// Commands represent intentions to change state and go through the full
/// command pipeline (validation, authorization, transaction, etc.).
/// </summary>
public interface ICommand;

/// <summary>
/// Marker interface for commands that return a result of type <typeparamref name="TResult"/>.
/// Commands represent intentions to change state and go through the full
/// command pipeline (validation, authorization, transaction, etc.).
/// </summary>
/// <typeparam name="TResult">The type of result returned by the command handler.</typeparam>
public interface ICommand<out TResult>;
