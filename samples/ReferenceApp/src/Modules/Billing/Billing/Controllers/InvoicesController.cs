using Billing.Contracts.DTOs;
using Billing.Features.GetInvoiceById;
using Billing.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nac.Cqrs.Dispatching;
using ReferenceApp.SharedKernel.Results;

namespace Billing.Controllers;

/// <summary>HTTP entry point for the Billing module.</summary>
[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController(ISender sender) : ControllerBase
{
    /// <summary>Returns an invoice by its identifier.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = BillingPermissionProvider.View)]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInvoiceById(Guid id, CancellationToken ct)
    {
        var query = new GetInvoiceByIdQuery(id);
        var result = await sender.SendAsync<Nac.Core.Results.Result<InvoiceResponse>>(query, ct);
        return result.ToActionResult();
    }
}
