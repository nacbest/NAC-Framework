using Orders.Domain;

namespace Orders.Infrastructure;

/// <summary>Persistence contract for the <see cref="Order"/> aggregate.</summary>
internal interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order> AddAsync(Order order, CancellationToken ct = default);
}
