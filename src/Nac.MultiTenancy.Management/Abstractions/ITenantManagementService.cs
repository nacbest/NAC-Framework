using Nac.Core.Results;
using Nac.MultiTenancy.Management.Dtos;

namespace Nac.MultiTenancy.Management.Abstractions;

/// <summary>
/// Façade over tenant CRUD + bulk lifecycle operations. All methods return
/// <see cref="Result"/> / <see cref="Result{T}"/> — domain errors never throw.
/// </summary>
public interface ITenantManagementService
{
    /// <summary>Creates a new tenant. 409 on duplicate identifier.</summary>
    Task<Result<TenantDto>> CreateAsync(CreateTenantRequest req, CancellationToken ct = default);

    /// <summary>Partially updates an existing tenant. 404 when not found, 409 on concurrency.</summary>
    Task<Result<TenantDto>> UpdateAsync(Guid id, UpdateTenantRequest req, CancellationToken ct = default);

    /// <summary>Soft-deletes a tenant. 404 when not found.</summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads a tenant by surrogate key.</summary>
    Task<Result<TenantDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads a tenant by its public identifier (slug).</summary>
    Task<Result<TenantDto>> GetByIdentifierAsync(string identifier, CancellationToken ct = default);

    /// <summary>Lists tenants with paging, search and active filter.</summary>
    Task<PagedResult<TenantDto>> ListAsync(TenantListQuery query, CancellationToken ct = default);

    /// <summary>Marks a tenant active. Idempotent.</summary>
    Task<Result> ActivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Marks a tenant inactive. Idempotent.</summary>
    Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>Best-effort bulk activate; per-id failures returned in <see cref="BulkResult.Failures"/>.</summary>
    Task<Result<BulkResult>> BulkActivateAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);

    /// <summary>Best-effort bulk deactivate.</summary>
    Task<Result<BulkResult>> BulkDeactivateAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);

    /// <summary>Best-effort bulk soft-delete.</summary>
    Task<Result<BulkResult>> BulkDeleteAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);
}
