using Nac.Core.Domain;
using Nac.Core.Primitives;
using Orders.Contracts.IntegrationEvents;

namespace Orders.Domain;

/// <summary>
/// Order aggregate root. TenantId implements <see cref="ITenantEntity"/> so
/// <c>TenantEntityInterceptor</c> auto-stamps the current tenant on insert.
/// </summary>
internal sealed class Order : AggregateRoot<Guid>, ITenantEntity
{
    private readonly List<OrderItem> _items = [];

    // Parameterless ctor for EF Core materialisation.
    private Order() { }

    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal Total { get; private set; }

    /// <inheritdoc cref="ITenantEntity"/>
    public string TenantId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    /// <summary>Read-only view of line items.</summary>
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Factory method — the only way to create a valid Order.
    /// Computes Total, stamps CreatedAt, raises <see cref="OrderCreatedEvent"/>
    /// (dual IDomainEvent + IIntegrationEvent) so OutboxInterceptor harvests it on SaveChanges.
    /// <paramref name="tenantId"/> is passed explicitly into the event payload so
    /// downstream Billing can set its own tenant context.
    /// </summary>
    public static Order Create(Guid customerId, IEnumerable<OrderItem> items, string tenantId)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0)
            throw new ArgumentException("An order must have at least one item.", nameof(items));

        var total = itemList.Sum(i => i.LineTotal);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            Total = total,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        order._items.AddRange(itemList);

        order.AddDomainEvent(new OrderCreatedEvent(
            OrderId: order.Id,
            CustomerId: customerId,
            TenantId: tenantId,
            Total: total,
            OccurredOn: order.CreatedAt,
            EventId: Guid.NewGuid()));

        return order;
    }
}
