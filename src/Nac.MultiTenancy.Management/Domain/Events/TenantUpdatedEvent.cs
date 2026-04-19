using Nac.Core.Abstractions.Events;
using Nac.Core.Primitives;

namespace Nac.MultiTenancy.Management.Domain.Events;

/// <summary>
/// Raised when any tenant property (Name, IsolationMode, ConnectionString, Properties)
/// is mutated post-creation. Coarse-grained on purpose — fine event splits would
/// over-couple consumers to internal state shape.
/// </summary>
public sealed record TenantUpdatedEvent(Guid Id, string Identifier)
    : IDomainEvent, IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
