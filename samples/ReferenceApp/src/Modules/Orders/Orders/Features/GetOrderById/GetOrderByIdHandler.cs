using Microsoft.EntityFrameworkCore;
using Nac.Core.Results;
using Nac.Cqrs.Queries;
using Orders.Contracts.DTOs;
using Orders.Infrastructure;

namespace Orders.Features.GetOrderById;

/// <summary>
/// Handles <see cref="GetOrderByIdQuery"/> via direct EF Core projection.
/// Bypasses the repository to avoid loading the full aggregate for a read-only query.
/// </summary>
internal sealed class GetOrderByIdHandler(OrdersDbContext db)
    : IQueryHandler<GetOrderByIdQuery, Result<OrderResponse>>
{
    public async ValueTask<Result<OrderResponse>> HandleAsync(
        GetOrderByIdQuery query,
        CancellationToken ct = default)
    {
        var response = await db.Orders
            .AsNoTracking()
            .Where(o => o.Id == query.Id)
            .Select(o => new OrderResponse(
                o.Id,
                o.CustomerId,
                o.Total,
                o.Status.ToString(),
                o.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return response is null
            ? Result<OrderResponse>.NotFound($"Order '{query.Id}' not found.")
            : Result<OrderResponse>.Success(response);
    }
}
