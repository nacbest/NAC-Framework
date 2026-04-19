namespace Nac.Cqrs.Markers;

/// <summary>
/// Marker interface that opts a command into the transaction pipeline behavior.
/// When a command implements this interface, <c>TransactionBehavior</c> automatically
/// calls <c>IUnitOfWork.SaveChangesAsync</c> after the handler completes successfully.
/// </summary>
/// <example>
/// <code>
/// public record CreateOrderCommand(Guid CustomerId, IReadOnlyList&lt;OrderLineDto&gt; Lines)
///     : ICommand&lt;Result&lt;Guid&gt;&gt;, ITransactionalCommand;
/// </code>
/// </example>
public interface ITransactionalCommand;
