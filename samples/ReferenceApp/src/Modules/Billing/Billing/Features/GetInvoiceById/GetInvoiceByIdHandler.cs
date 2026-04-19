using Billing.Contracts.DTOs;
using Billing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Nac.Core.Results;
using Nac.Cqrs.Queries;

namespace Billing.Features.GetInvoiceById;

/// <summary>
/// Handles <see cref="GetInvoiceByIdQuery"/> via direct EF Core projection.
/// Bypasses any aggregate loading — read-only, no side effects.
/// </summary>
internal sealed class GetInvoiceByIdHandler(BillingDbContext db)
    : IQueryHandler<GetInvoiceByIdQuery, Result<InvoiceResponse>>
{
    public async ValueTask<Result<InvoiceResponse>> HandleAsync(
        GetInvoiceByIdQuery query,
        CancellationToken ct = default)
    {
        var response = await db.Invoices
            .AsNoTracking()
            .Where(i => i.Id == query.Id)
            .Select(i => new InvoiceResponse(
                i.Id,
                i.OrderId,
                i.Amount,
                i.Status.ToString(),
                i.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return response is null
            ? Result<InvoiceResponse>.NotFound($"Invoice '{query.Id}' not found.")
            : Result<InvoiceResponse>.Success(response);
    }
}
