using Nac.Core.Abstractions.Events;
using Nac.Core.Primitives;

namespace Nac.MultiTenancy.Management.Domain.Events;

/// <summary>
/// Raised when a tenant is soft-deleted. Consumers should treat as a logical
/// tombstone — the row is preserved in storage for audit but no longer resolves.
/// </summary>
public sealed record TenantDeletedEvent(Guid Id, string Identifier)
    : IDomainEvent, IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
