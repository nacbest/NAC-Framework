using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nac.MultiTenancy.Management.Abstractions;
using Nac.MultiTenancy.Management.Authorization;
using Nac.MultiTenancy.Management.Dtos;
using Nac.MultiTenancy.Management.Internal;

namespace Nac.MultiTenancy.Management.Controllers;

/// <summary>
/// Admin-facing tenant lifecycle REST API. All endpoints require the
/// <c>Tenants.Manage</c> policy (claim-based) AND the <see cref="HostAdminOnlyFilter"/>
/// (host-realm caller). Bulk endpoints return 207 (Multi-Status) when partial.
/// </summary>
[ApiController]
[Route("api/admin/tenants")]
[Authorize(Policy = "Tenants.Manage")]
[ServiceFilter(typeof(HostAdminOnlyFilter))]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantManagementService _service;

    public TenantsController(ITenantManagementService service) => _service = service;

    /// <summary>Creates a new tenant. Returns 201 + Location.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : result.ToActionResult(this);
    }

    /// <summary>Lists tenants with paging.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] TenantListQuery query, CancellationToken ct)
    {
        var paged = await _service.ListAsync(query, ct);
        return Ok(paged);
    }

    /// <summary>Loads a tenant by surrogate id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult(this);

    /// <summary>Loads a tenant by public identifier (slug).</summary>
    [HttpGet("by-identifier/{identifier}")]
    public async Task<IActionResult> GetByIdentifier(string identifier, CancellationToken ct)
        => (await _service.GetByIdentifierAsync(identifier, ct)).ToActionResult(this);

    /// <summary>Partial update.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantRequest request, CancellationToken ct)
        => (await _service.UpdateAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Soft-deletes the tenant.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult(this);

    /// <summary>Activates the tenant. Idempotent.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
        => (await _service.ActivateAsync(id, ct)).ToActionResult(this);

    /// <summary>Deactivates the tenant. Idempotent.</summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        => (await _service.DeactivateAsync(id, ct)).ToActionResult(this);

    /// <summary>Bulk activate. Returns 207 when any failures.</summary>
    [HttpPost("bulk/activate")]
    public Task<IActionResult> BulkActivate([FromBody] BulkTenantRequest request, CancellationToken ct)
        => HandleBulk(_service.BulkActivateAsync, request, ct);

    /// <summary>Bulk deactivate. Returns 207 when any failures.</summary>
    [HttpPost("bulk/deactivate")]
    public Task<IActionResult> BulkDeactivate([FromBody] BulkTenantRequest request, CancellationToken ct)
        => HandleBulk(_service.BulkDeactivateAsync, request, ct);

    /// <summary>Bulk soft-delete. Returns 207 when any failures.</summary>
    [HttpPost("bulk/delete")]
    public Task<IActionResult> BulkDelete([FromBody] BulkTenantRequest request, CancellationToken ct)
        => HandleBulk(_service.BulkDeleteAsync, request, ct);

    private async Task<IActionResult> HandleBulk(
        Func<IReadOnlyList<Guid>, CancellationToken, Task<Nac.Core.Results.Result<BulkResult>>> op,
        BulkTenantRequest request,
        CancellationToken ct)
    {
        if (request.Ids is null || request.Ids.Count == 0)
            return BadRequest("Ids must be non-empty.");
        var result = await op(request.Ids, ct);
        if (!result.IsSuccess) return result.ToActionResult(this);
        var body = result.Value;
        return body.Failures.Count > 0
            ? StatusCode(StatusCodes.Status207MultiStatus, body)
            : Ok(body);
    }
}
