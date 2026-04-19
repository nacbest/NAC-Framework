using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nac.Core.Results;
using Nac.MultiTenancy.Management.Abstractions;
using Nac.MultiTenancy.Management.Domain;
using Nac.MultiTenancy.Management.Dtos;
using Nac.MultiTenancy.Management.Persistence;

namespace Nac.MultiTenancy.Management.Services;

/// <summary>
/// Default <see cref="ITenantManagementService"/> implementation.
/// Orchestrates aggregate mutations, runs FluentValidation, encrypts connection
/// strings via DataProtection, and invalidates the tenant cache after each
/// successful save. Domain events flow to the outbox automatically because the
/// DbContext inherits <c>NacDbContext</c>.
/// </summary>
internal sealed class TenantManagementService : ITenantManagementService
{
    private readonly TenantManagementDbContext _db;
    private readonly IValidator<CreateTenantRequest> _createValidator;
    private readonly IValidator<UpdateTenantRequest> _updateValidator;
    private readonly IDataProtector _protector;
    private readonly ITenantCacheInvalidator _cache;
    private readonly TenantManagementOptions _options;
    private readonly ILogger<TenantManagementService> _logger;

    public TenantManagementService(
        TenantManagementDbContext db,
        IValidator<CreateTenantRequest> createValidator,
        IValidator<UpdateTenantRequest> updateValidator,
        IDataProtectionProvider dataProtectionProvider,
        ITenantCacheInvalidator cache,
        IOptions<TenantManagementOptions> options,
        ILogger<TenantManagementService> logger)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _protector = dataProtectionProvider.CreateProtector(
            EncryptedConnectionStringResolver.ProtectorPurpose);
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<TenantDto>> CreateAsync(CreateTenantRequest req, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(req, ct);
        if (!validation.IsValid)
            return Result<TenantDto>.Invalid(ToValidationErrors(validation));

        var exists = await _db.Tenants.AnyAsync(t => t.Identifier == req.Identifier, ct);
        if (exists)
            return Result<TenantDto>.Conflict($"Tenant identifier '{req.Identifier}' already exists.");

        var cipher = Protect(req.ConnectionString);
        var tenant = Tenant.Create(
            Guid.NewGuid(), req.Identifier, req.Name, req.IsolationMode, cipher, req.Properties);

        _db.Tenants.Add(tenant);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await IsDuplicateIdentifierAsync(req.Identifier, ct))
                return Result<TenantDto>.Conflict($"Tenant identifier '{req.Identifier}' already exists.");
            throw;
        }

        _cache.Invalidate(tenant.Identifier);
        _logger.LogInformation("Created tenant {TenantId} ({Identifier})", tenant.Id, tenant.Identifier);
        return Result<TenantDto>.Success(TenantMapper.ToDto(tenant));
    }

    /// <inheritdoc />
    public async Task<Result<TenantDto>> UpdateAsync(Guid id, UpdateTenantRequest req, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(req, ct);
        if (!validation.IsValid)
            return Result<TenantDto>.Invalid(ToValidationErrors(validation));

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null) return Result<TenantDto>.NotFound($"Tenant '{id}' not found.");

        if (req.Name is not null) tenant.Rename(req.Name);

        var newCipher = req.ConnectionString is null ? null : Protect(req.ConnectionString);

        if (req.IsolationMode.HasValue)
        {
            try
            {
                tenant.ChangeIsolation(req.IsolationMode.Value, newCipher);
            }
            catch (InvalidOperationException ex)
            {
                return Result<TenantDto>.Invalid(new ValidationError(nameof(req.ConnectionString), ex.Message));
            }
        }
        else if (newCipher is not null)
        {
            tenant.SetEncryptedConnectionString(newCipher);
        }

        if (req.Properties is { Count: > 0 }) tenant.SetProperties(req.Properties);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<TenantDto>.Conflict("Concurrent modification detected. Reload and retry.");
        }

        _cache.Invalidate(tenant.Identifier);
        return Result<TenantDto>.Success(TenantMapper.ToDto(tenant));
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null) return Result.NotFound($"Tenant '{id}' not found.");

        tenant.MarkDeleted();
        await _db.SaveChangesAsync(ct);
        _cache.Invalidate(tenant.Identifier);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<TenantDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return tenant is null
            ? Result<TenantDto>.NotFound($"Tenant '{id}' not found.")
            : Result<TenantDto>.Success(TenantMapper.ToDto(tenant));
    }

    /// <inheritdoc />
    public async Task<Result<TenantDto>> GetByIdentifierAsync(string identifier, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Identifier == identifier, ct);
        return tenant is null
            ? Result<TenantDto>.NotFound($"Tenant '{identifier}' not found.")
            : Result<TenantDto>.Success(TenantMapper.ToDto(tenant));
    }

    /// <inheritdoc />
    public async Task<PagedResult<TenantDto>> ListAsync(TenantListQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize <= 0 ? _options.DefaultPageSize : query.PageSize,
            1, _options.MaxPageSize);

        IQueryable<Tenant> q = _db.Tenants.AsNoTracking();
        if (query.IsActive.HasValue)
            q = q.Where(t => t.IsActive == query.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLowerInvariant();
            q = q.Where(t => t.Identifier.ToLower().Contains(s) || t.Name.ToLower().Contains(s));
        }

        var total = await q.LongCountAsync(ct);
        var items = await q.OrderBy(t => t.Identifier)
            .Skip((page - 1) * size).Take(size)
            .ToListAsync(ct);

        return new PagedResult<TenantDto>(items.Select(TenantMapper.ToDto).ToList(), page, size, total);
    }

    /// <inheritdoc />
    public Task<Result> ActivateAsync(Guid id, CancellationToken ct = default)
        => MutateAsync(id, t => t.Activate(), ct);

    /// <inheritdoc />
    public Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default)
        => MutateAsync(id, t => t.Deactivate(), ct);

    /// <inheritdoc />
    public Task<Result<BulkResult>> BulkActivateAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
        => BulkMutateAsync(ids, t => t.Activate(), ct);

    /// <inheritdoc />
    public Task<Result<BulkResult>> BulkDeactivateAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
        => BulkMutateAsync(ids, t => t.Deactivate(), ct);

    /// <inheritdoc />
    public Task<Result<BulkResult>> BulkDeleteAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
        => BulkMutateAsync(ids, t => t.MarkDeleted(), ct);

    private async Task<Result> MutateAsync(Guid id, Action<Tenant> mutate, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null) return Result.NotFound($"Tenant '{id}' not found.");
        mutate(tenant);
        await _db.SaveChangesAsync(ct);
        _cache.Invalidate(tenant.Identifier);
        return Result.Success();
    }

    private async Task<Result<BulkResult>> BulkMutateAsync(
        IReadOnlyList<Guid> ids, Action<Tenant> mutate, CancellationToken ct)
    {
        if (ids is null || ids.Count == 0)
            return Result<BulkResult>.Invalid(new ValidationError(nameof(ids), "At least one id is required."));
        if (ids.Count > _options.MaxBulkSize)
            return Result<BulkResult>.Invalid(new ValidationError(
                nameof(ids), $"Request exceeds MaxBulkSize ({_options.MaxBulkSize})."));

        var requested = ids.Distinct().ToList();
        var failures = new Dictionary<Guid, string>();
        var found = await _db.Tenants.Where(t => requested.Contains(t.Id)).ToListAsync(ct);
        var foundIds = found.Select(t => t.Id).ToHashSet();

        foreach (var missing in requested.Where(id => !foundIds.Contains(id)))
            failures[missing] = "not found";

        var touched = new List<Tenant>(found.Count);
        foreach (var tenant in found)
        {
            try
            {
                mutate(tenant);
                touched.Add(tenant);
            }
            catch (Exception ex)
            {
                failures[tenant.Id] = ex.Message;
            }
        }

        var succeeded = 0;
        if (touched.Count > 0)
        {
            try
            {
                await _db.SaveChangesAsync(ct);
                succeeded = touched.Count;
                foreach (var t in touched) _cache.Invalidate(t.Identifier);
            }
            catch (DbUpdateException ex)
            {
                foreach (var t in touched)
                    failures[t.Id] = ex.GetBaseException().Message;
            }
        }

        return Result<BulkResult>.Success(new BulkResult(requested.Count, succeeded, failures));
    }

    private string? Protect(string? plaintext) =>
        string.IsNullOrEmpty(plaintext) ? null : _protector.Protect(plaintext);

    private async Task<bool> IsDuplicateIdentifierAsync(string identifier, CancellationToken ct) =>
        await _db.Tenants.AsNoTracking().AnyAsync(t => t.Identifier == identifier, ct);

    private static ValidationError[] ToValidationErrors(FluentValidation.Results.ValidationResult result) =>
        result.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage)).ToArray();
}
