using Billing.Contracts.DTOs;
using Nac.Core.Results;
using Nac.Cqrs.Queries;

namespace Billing.Features.GetInvoiceById;

/// <summary>Query to fetch a single invoice by its identifier.</summary>
internal sealed record GetInvoiceByIdQuery(Guid Id) : IQuery<Result<InvoiceResponse>>;
