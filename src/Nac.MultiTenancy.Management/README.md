# Nac.MultiTenancy.Management

Admin-facing tenant lifecycle management for NAC Framework. Provides opinionated CRUD operations, encrypted per-tenant connection strings, and outbox-emitted domain events on top of `Nac.MultiTenancy`.

## Installation

```bash
dotnet add package Nac.MultiTenancy.Management
```

**Prerequisite:** Already using `Nac.MultiTenancy` and `Nac.Persistence` in your application.

## Quick Start

### 1. Register the Module

In `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Prerequisite: Add multi-tenancy resolution
builder.Services.AddNacMultiTenancy(opts =>
{
    opts.DefaultResolutionStrategy = TenantResolutionStrategy.Header;
});

// Register tenant management
builder.Services.AddNacTenantManagement(opts =>
{
    opts.UseDbContext(db =>
    {
        db.UseSqlServer(builder.Configuration.GetConnectionString("Registry"));
    });
});

// ... rest of configuration
```

### 2. Run Migrations

Create and apply EF Core migrations for the registry database:

```bash
dotnet ef migrations add InitialCreate --project src/YourApp/YourApp.csproj
dotnet ef database update
```

## API Endpoints

All endpoints require `[Authorize(Policy = "Tenants.Manage")]` and host-admin context (non-null `ICurrentUser.TenantId` rejected).

| Verb | Path | Description |
|------|------|-------------|
| POST | `/api/admin/tenants` | Create tenant |
| GET | `/api/admin/tenants` | List with pagination |
| GET | `/api/admin/tenants/{id:guid}` | Get by surrogate ID |
| GET | `/api/admin/tenants/by-identifier/{identifier}` | Get by public identifier |
| PUT | `/api/admin/tenants/{id:guid}` | Update (partial) |
| DELETE | `/api/admin/tenants/{id:guid}` | Soft-delete |
| POST | `/api/admin/tenants/{id:guid}/activate` | Activate (idempotent) |
| POST | `/api/admin/tenants/{id:guid}/deactivate` | Deactivate (idempotent) |
| POST | `/api/admin/tenants/bulk/activate` | Bulk activate (max 100 IDs) |
| POST | `/api/admin/tenants/bulk/deactivate` | Bulk deactivate (max 100 IDs) |
| POST | `/api/admin/tenants/bulk/delete` | Bulk delete (max 100 IDs) |

## Key Features

### Encrypted Connection Strings

Per-tenant connection strings are encrypted at rest using Microsoft's `DataProtection` API with a purpose constant:

```csharp
const string Purpose = "Nac.MultiTenancy.Management.ConnectionString";
```

**Critical:** DataProtection keys must persist across application restarts. Without persistence, encrypted ciphertexts become unreadable after restart.

Configure key persistence in `Program.cs`:

```csharp
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/path/to/persistent/key/store"))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
```

See [Microsoft DataProtection docs](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/) for production key management (Azure Key Vault, etc.).

### Domain Events

All tenant mutations emit domain events published via the Outbox pattern:

- `TenantCreatedEvent` — Tenant created
- `TenantUpdatedEvent` — Tenant updated
- `TenantDeletedEvent` — Tenant soft-deleted
- `TenantActivatedEvent` — Tenant activated
- `TenantDeactivatedEvent` — Tenant deactivated

All events implement both `IDomainEvent` and `IIntegrationEvent` for outbox publication.

### Tenant Aggregate

`Tenant` is an `AggregateRoot<Guid>` with:

- **Auditing:** `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` (via `IAuditableEntity`)
- **Soft Delete:** Logical deletion tracking (via `ISoftDeletable`)
- **Isolation:** Encrypted connection string (ciphertext stored in registry DB)
- **Status:** Active/Inactive state machine

### EF-Backed Registry

`TenantManagementDbContext` is the centralized registry database—NOT a multi-tenant context. It stores all tenant records with encrypted connection strings.

The `EfCoreTenantStore` implementation:

- **Caches:** 10-minute sliding window per tenant (via `IMemoryCache`)
- **Returns:** Ciphertext connection strings (decryption deferred to `EncryptedConnectionStringResolver`)
- **Invalidates:** On create/update/delete/activate/deactivate via `ITenantCacheInvalidator`

### Cache Invalidation

Manual cache invalidation is available via `ITenantCacheInvalidator`:

```csharp
var invalidator = serviceProvider.GetRequiredService<ITenantCacheInvalidator>();
await invalidator.InvalidateTenantAsync(tenantId);
await invalidator.InvalidateAllAsync();
```

Automatic invalidation occurs on all mutations.

## Configuration

### TenantManagementOptions

```csharp
opts.UseDbContext(dbAction)     // REQUIRED: Configure registry DB
opts.PermissionName             // Default: "Tenants.Manage"
opts.RoutePrefix                // Default: "api/admin/tenants"
```

## Request/Response DTOs

### CreateTenantRequest

```csharp
{
  "name": "string",
  "identifier": "string",              // Public identifier (unique, slug-like)
  "connectionString": "string",        // Will be encrypted at rest
  "isolationMode": "SingleDatabase",   // or "MultiDatabase"
  "isActive": true
}
```

### TenantResponse

```csharp
{
  "id": "guid",
  "name": "string",
  "identifier": "string",
  "isolationMode": "string",
  "isActive": bool,
  "createdAt": "ISO 8601",
  "createdBy": "string",
  "modifiedAt": "ISO 8601",
  "modifiedBy": "string"
}
```

Note: Connection strings are never returned in responses.

### TenantListQuery

```csharp
{
  "pageNumber": 1,              // Default: 1
  "pageSize": 20,               // Default: 20, max 100
  "searchTerm": "string"        // Optional: filter by name or identifier
}
```

## Testing

The module includes 38 passing unit tests covering:

- Domain aggregate state transitions
- EF-backed store with caching and invalidation
- Encrypted resolver round-trip
- All 11 REST endpoints with authorization
- Bulk operations with partial failure handling
- Outbox event emission for all mutations
- Soft-delete and audit interceptor integration

Run tests:

```bash
dotnet test src/Nac.MultiTenancy.Management.Tests/
```

## Dependencies

- `Nac.Core` (L0) — DDD primitives, module system
- `Nac.MultiTenancy` (L2) — `ITenantStore`, `ITenantConnectionStringResolver` abstractions
- `Nac.Persistence` (L2) — DbContext, EF Core interceptors, Outbox pattern
- `Microsoft.AspNetCore.DataProtection` — Connection string encryption
- `Microsoft.Extensions.Caching.Memory` — 10-minute cache for tenant lookups
- `FluentValidation` — Request validation

## Version History

- **v2.1.0** (2026-04-19) — Initial release with full tenant lifecycle, encryption, and outbox events
