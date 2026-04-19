using Nac.Core.Primitives;
using Nac.MultiTenancy.Management.Abstractions;
using Nac.MultiTenancy.Management.Domain.Events;

namespace Nac.MultiTenancy.Management.Domain;

/// <summary>
/// Tenant aggregate root — central record of every tenant managed by the host.
/// All mutations occur through behaviour methods that emit matching domain events;
/// public setters are deliberately absent to guarantee invariants and event emission.
/// Encryption of <see cref="EncryptedConnectionString"/> is performed at the service
/// boundary, never inside the aggregate (keeps domain pure and easily testable).
/// </summary>
public sealed class Tenant : AggregateRoot<Guid>, IAuditableEntity, ISoftDeletable
{
    /// <summary>Stable, URL-safe slug used by clients to address the tenant.</summary>
    public string Identifier { get; private set; } = default!;

    /// <summary>Human-readable display name.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Strategy used to isolate this tenant's data.</summary>
    public TenantIsolationMode IsolationMode { get; private set; }

    /// <summary>
    /// Encrypted connection string ciphertext (base64). Always opaque to the aggregate;
    /// the service layer is responsible for protect / unprotect via DataProtection.
    /// </summary>
    public string? EncryptedConnectionString { get; private set; }

    /// <summary>Whether the tenant is allowed to resolve at runtime.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Free-form metadata; case-insensitive keys.</summary>
    public Dictionary<string, string?> Properties { get; private set; }
        = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc />
    public string? CreatedBy { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    /// <summary>EF Core constructor — never call from application code.</summary>
    private Tenant() { }

    /// <summary>
    /// Creates a new tenant aggregate. Validation of <paramref name="identifier"/> shape
    /// is delegated to FluentValidation; aggregate enforces only structural invariants.
    /// </summary>
    public static Tenant Create(
        Guid id,
        string identifier,
        string name,
        TenantIsolationMode mode,
        string? encryptedConnectionString,
        IDictionary<string, string?>? properties,
        Guid? createdByUserId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (id == Guid.Empty) throw new ArgumentException("Id required", nameof(id));

        var tenant = new Tenant
        {
            Id = id,
            Identifier = identifier,
            Name = name,
            IsolationMode = mode,
            EncryptedConnectionString = encryptedConnectionString,
            IsActive = true,
        };
        if (properties is { Count: > 0 })
        {
            foreach (var kv in properties)
                tenant.Properties[kv.Key] = kv.Value;
        }
        tenant.AddDomainEvent(new TenantCreatedEvent(id, identifier, name, mode, createdByUserId));
        return tenant;
    }

    /// <summary>Renames the tenant. Emits <see cref="TenantUpdatedEvent"/> when the name actually changes.</summary>
    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (string.Equals(Name, name, StringComparison.Ordinal)) return;
        Name = name;
        AddDomainEvent(new TenantUpdatedEvent(Id, Identifier));
    }

    /// <summary>Switches isolation strategy and (optionally) the encrypted connection string.</summary>
    public void ChangeIsolation(TenantIsolationMode mode, string? encryptedConnectionString)
    {
        if (mode == TenantIsolationMode.Database && string.IsNullOrWhiteSpace(encryptedConnectionString)
            && string.IsNullOrWhiteSpace(EncryptedConnectionString))
        {
            throw new InvalidOperationException(
                "Database isolation requires a connection string.");
        }
        var changed = mode != IsolationMode
            || (encryptedConnectionString is not null
                && encryptedConnectionString != EncryptedConnectionString);
        if (!changed) return;
        IsolationMode = mode;
        if (encryptedConnectionString is not null)
            EncryptedConnectionString = encryptedConnectionString;
        AddDomainEvent(new TenantUpdatedEvent(Id, Identifier));
    }

    /// <summary>Replaces the encrypted connection string ciphertext (or clears it).</summary>
    public void SetEncryptedConnectionString(string? cipher)
    {
        if (string.Equals(EncryptedConnectionString, cipher, StringComparison.Ordinal)) return;
        EncryptedConnectionString = cipher;
        AddDomainEvent(new TenantUpdatedEvent(Id, Identifier));
    }

    /// <summary>Merges the supplied dictionary into <see cref="Properties"/> (overwrites by key).</summary>
    public void SetProperties(IDictionary<string, string?> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        if (properties.Count == 0) return;
        foreach (var kv in properties)
            Properties[kv.Key] = kv.Value;
        AddDomainEvent(new TenantUpdatedEvent(Id, Identifier));
    }

    /// <summary>Marks the tenant active. Idempotent.</summary>
    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        AddDomainEvent(new TenantActivatedEvent(Id));
    }

    /// <summary>Marks the tenant inactive. Idempotent.</summary>
    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        AddDomainEvent(new TenantDeactivatedEvent(Id));
    }

    /// <summary>
    /// Marks the tenant as deleted (soft-delete). The
    /// <c>SoftDeleteInterceptor</c> in <c>Nac.Persistence</c> converts the
    /// EF delete operation into an UPDATE.
    /// </summary>
    public void MarkDeleted()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        AddDomainEvent(new TenantDeletedEvent(Id, Identifier));
    }
}
