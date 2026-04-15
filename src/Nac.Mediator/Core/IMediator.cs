using Nac.Core.Messaging;

namespace Nac.Mediator.Core;

/// <summary>
/// Central mediator for dispatching commands, queries, and notifications through the pipeline.
/// Commands and queries have separate pipelines with different behavior chains.
/// </summary>
public interface IMediator
{
    /// <summary>Sends a void command through the command pipeline.</summary>
    Task SendAsync(ICommand command, CancellationToken ct = default);

    /// <summary>Sends a command through the command pipeline and returns the result.</summary>
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default);

    /// <summary>Sends a query through the query pipeline and returns the result.</summary>
    Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default);

    /// <summary>
    /// Publishes a notification to all registered handlers (one-to-many, in-process).
    /// Handlers execute sequentially. If any handler throws, remaining handlers are skipped.
    /// </summary>
    Task PublishAsync(INotification notification, CancellationToken ct = default);
}
