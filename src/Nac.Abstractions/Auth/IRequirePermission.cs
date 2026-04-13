namespace Nac.Abstractions.Auth;

/// <summary>
/// Marker interface for commands/queries that require a specific permission.
/// The authorization pipeline behavior checks this before invoking the handler.
/// </summary>
/// <example>
/// <code>
/// public record CreateOrderCommand(...) : ICommand&lt;Guid&gt;, IRequirePermission
/// {
///     public string Permission => "orders.create";
/// }
/// </code>
/// </example>
public interface IRequirePermission
{
    /// <summary>
    /// Permission string required to execute this command/query.
    /// Format: <c>module.resource.action</c> — supports wildcard (<c>orders.*</c>).
    /// </summary>
    string Permission { get; }
}
