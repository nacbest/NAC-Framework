using Microsoft.EntityFrameworkCore;
using Orders.Domain;

namespace Orders.Infrastructure;

/// <summary>EF Core implementation of <see cref="IOrderRepository"/>.</summary>
internal sealed class OrderRepository(OrdersDbContext db) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Orders
            .Include("Items") // shadow nav — OwnsMany backed by private field
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Order> AddAsync(Order order, CancellationToken ct = default)
    {
        var entry = await db.Orders.AddAsync(order, ct);
        return entry.Entity;
    }
}
