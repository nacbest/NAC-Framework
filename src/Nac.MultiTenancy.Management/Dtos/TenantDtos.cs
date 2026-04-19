using Nac.MultiTenancy.Management.Abstractions;

namespace Nac.MultiTenancy.Management.Dtos;

/// <summary>Request payload for creating a new tenant.</summary>
public sealed record CreateTenantRequest(
    string Identifier,
    string Name,
    TenantIsolationMode IsolationMode,
    string? ConnectionString,
    Dictionary<string, string?>? Properties);

/// <summary>
/// Request payload for partially updating an existing tenant. Each property is
/// optional; <see langword="null"/> means "do not change".
/// </summary>
public sealed record UpdateTenantRequest(
    string? Name,
    TenantIsolationMode? IsolationMode,
    string? ConnectionString,
    Dictionary<string, string?>? Properties);

/// <summary>
/// Read-side projection of a tenant. Never includes the connection string —
/// neither plaintext nor ciphertext — to keep secrets out of API responses.
/// </summary>
public sealed record TenantDto(
    Guid Id,
    string Identifier,
    string Name,
    TenantIsolationMode IsolationMode,
    bool IsActive,
    IReadOnlyDictionary<string, string?> Properties,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>Query string parameters for listing tenants.</summary>
public sealed record TenantListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null);

/// <summary>Bulk operation payload — at most <c>MaxBulkSize</c> distinct ids.</summary>
public sealed record BulkTenantRequest(IReadOnlyList<Guid> Ids);

/// <summary>Outcome of a bulk operation; failures are reported per-id.</summary>
public sealed record BulkResult(
    int TotalRequested,
    int Succeeded,
    IReadOnlyDictionary<Guid, string> Failures);

/// <summary>Generic paged-result wrapper used by the list endpoint.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount)
{
    /// <summary>Calculated total number of pages for the current page size.</summary>
    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling((double)TotalCount / PageSize);
}
