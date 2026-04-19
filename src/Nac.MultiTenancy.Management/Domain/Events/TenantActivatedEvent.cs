using Nac.Core.Abstractions.Events;
using Nac.Core.Primitives;

namespace Nac.MultiTenancy.Management.Domain.Events;

/// <summary>Raised when a tenant transitions from inactive to active.</summary>
public sealed record TenantActivatedEvent(Guid Id) : IDomainEvent, IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
