# ReferenceApp

Reference consumer project for **NACFramework**. Demonstrates a Modular Monolith with two business modules (`Orders`, `Billing`) and an end-to-end cross-module integration event flow. Copy + rename this solution to start a new NAC-based service — this sample is the blueprint in lieu of a `dotnet new` template.

> **Architecture spec:** [`NAC-Consumer-Project-Architecture.md`](./NAC-Consumer-Project-Architecture.md) — the canonical blueprint every consumer project must follow (module layout, `[DependsOn]` graph, CQRS pipeline, permissions, multi-tenancy, event flow, naming). Bundled here so it travels with clone+rename.

---

## 1. Prerequisites

- .NET 10 SDK
- A running Postgres 17 instance reachable from the host (defaults assume `localhost:5432`, user/password `admin/123456`, database `referenceapp` — override via `ConnectionStrings:Default` if your instance differs)
- (Optional) JetBrains Rider / Visual Studio 2026

## 2. Quick start

```bash
cd samples/ReferenceApp
# Ensure Postgres is running and the `referenceapp` database exists, e.g.:
#   createdb -h localhost -p 5432 -U admin referenceapp
dotnet build
dotnet run --project src/Host
# Browse http://localhost:5000/openapi/v1.json (OpenAPI document)
# Health: http://localhost:5000/healthz
```

The host auto-applies migrations (Development only) and seeds an admin user:
- `admin@referenceapp.local` / `Admin123!` — has all permissions across Orders + Billing.

Smoke test (end-to-end event flow):

```bash
# 1. Login
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@referenceapp.local","password":"Admin123!"}' | jq -r .token)

# 2. Create an order
curl -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"items":[{"productId":"'"$(uuidgen)"'","quantity":2,"unitPrice":50}]}'

# 3. After ~5s, Billing upserts Customer + creates Invoice automatically
```

## 3. Architecture

```
┌─────────────────────── Host (composition root) ───────────────────────┐
│  Program.cs: AddNacWebApi → AddNacPersistence<AppDbContext>           │
│             → AddNacMultiTenancy → AddNacCqrs → AddNacEventBus         │
│             → AddNacIdentity<AppDbContext> → AddNacCaching             │
│             → AddNacApplication<AppRootModule>                         │
│                                                                        │
│  AppRootModule [DependsOn]: NacWebApi, NacIdentity, NacMultiTenancy,  │
│                             BillingModule, OrdersModule                │
└────────────────────────────────┬───────────────────────────────────────┘
                                 │
         ┌───────────────────────┼───────────────────────┐
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ OrdersModule    │    │ BillingModule   │    │ Nac.Identity    │
│ schema: orders  │    │ schema: billing │    │ schema: identity│
│ OrdersDbContext │    │ BillingDbContext│    │ AppDbContext    │
└────────┬────────┘    └────────▲────────┘    └─────────────────┘
         │                      │
         │ OrderCreatedEvent    │ IEventHandler<OrderCreatedEvent>
         │ (IDomainEvent +      │ upserts Customer + Invoice
         │  IIntegrationEvent)  │
         ▼                      │
   OutboxInterceptor            │
   (pre-save, same tx) ─────────┤
         │                      │
   orders.__outbox_events       │
         │                      │
   OutboxWorker (5s poll) ──────┘
         │
   InMemoryEventBus channel ────→ handler dispatch
```

One Postgres database, three schemas. Modules communicate through `Orders.Contracts` (DTOs + integration events) only — no cross-module internal references.

## 4. Project layout

```
samples/ReferenceApp/
├── Directory.Build.props          # TargetFramework net10.0, nullable, warnings-as-errors
├── Directory.Packages.props       # Central package management
├── ReferenceApp.slnx              # Solution
└── src/
    ├── BuildingBlocks/
    │   └── ReferenceApp.SharedKernel/
    │       ├── Authorization/     # PermissionAuthorizationPolicyProvider
    │       └── Results/           # ResultExtensions.ToActionResult
    ├── Host/
    │   ├── Program.cs             # Composition root
    │   ├── AppRootModule.cs       # [DependsOn] graph
    │   ├── AppDbContext.cs        # Identity context, schema "identity"
    │   ├── Controllers/           # AuthController (register + login)
    │   ├── Seeding/               # AdminSeeder
    │   ├── Migrations/            # InitIdentity
    │   └── appsettings*.json
    └── Modules/
        ├── Orders/
        │   ├── Orders.Contracts/  # DTOs + OrderCreatedEvent
        │   └── Orders/            # Domain + Features + Controllers + Infrastructure
        └── Billing/
            ├── Billing.Contracts/ # DTOs + InvoiceIssuedEvent
            └── Billing/           # Domain + OrderCreatedEventHandler + Controllers
```

## 5. Key patterns

| Pattern | Where to look |
|---|---|
| Explicit module registration | `src/Host/AppRootModule.cs` |
| Per-module `DbContext` + schema | `src/Modules/Orders/Orders/Infrastructure/OrdersDbContext.cs`, `src/Modules/Billing/Billing/Infrastructure/BillingDbContext.cs` |
| Merged domain + integration event (single class, two interfaces) | `src/Modules/Orders/Orders.Contracts/IntegrationEvents/OrderCreatedEvent.cs` |
| Outbox auto-harvest on SaveChanges | framework `OutboxInterceptor` picks up events implementing `IIntegrationEvent` |
| Cross-module event handler | `src/Modules/Billing/Billing/Features/EventHandlers/OrderCreatedEventHandler.cs` |
| Idempotency (outbox redelivery) | unique index on `Invoice.OrderId` + `AnyAsync` guard in handler |
| Tenant propagation across modules | payload carries `TenantId`; handler sets `ITenantContext` before DB work |
| Permission-based auth | `src/BuildingBlocks/ReferenceApp.SharedKernel/Authorization/PermissionAuthorizationPolicyProvider.cs` + `[Authorize(Policy="Orders.Create")]` on controllers |
| Permission definitions | `src/Modules/Orders/Orders/Permissions/OrderPermissionProvider.cs` |
| CQRS explicit pipeline | `AddNacCqrs` in `Program.cs` (logging + validation + transaction + caching behaviors) |
| Controller-based endpoints | `OrdersController`, `InvoicesController`, `AuthController` |
| Admin role + permission grant seeding | `src/Host/Seeding/AdminSeeder.cs` |

## 6. How to add a new module

1. Copy `src/Modules/Orders/` → `src/Modules/YourModule/` and rename projects/classes.
2. Rewrite `YourModule.Contracts/DTOs/` and `IntegrationEvents/` for your domain.
3. Rewrite domain (aggregate), features (commands + queries), infrastructure (DbContext, EF configs, migration).
4. Register permissions in a `YourPermissionProvider : IPermissionDefinitionProvider`.
5. Add `[DependsOn(typeof(YourModule))]` to `AppRootModule` and a `ProjectReference` in `Host.csproj`. Grant permissions in `AdminSeeder`.
6. Generate migration:
   ```bash
   dotnet ef migrations add InitYourModule \
     --project src/Modules/YourModule/YourModule \
     --startup-project src/Host \
     --context YourDbContext \
     --output-dir Infrastructure/Migrations
   ```

## 7. Running tests

```bash
# From repo root
dotnet test tests/ReferenceApp.IntegrationTests
```

Uses `WebApplicationFactory` + `Testcontainers.PostgreSql` + `Respawn`. First run pulls `postgres:17` (~30s); cached afterwards. Full suite ~17s.

## 8. Configuration

| appsettings key | Env var override | Notes |
|---|---|---|
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | Postgres connection |
| `Jwt:SecretKey` | `Jwt__SecretKey` | Must be ≥32 chars |
| `Jwt:Issuer` / `Jwt:Audience` | `Jwt__Issuer` / `Jwt__Audience` | Must match client expectations |
| `Jwt:ExpirationMinutes` | `Jwt__ExpirationMinutes` | Access-token TTL |
| `MultiTenancy:DefaultTenantId` | `MultiTenancy__DefaultTenantId` | Used when `X-Tenant-Id` header missing |

## 9. Limitations (roadmap)

The sample intentionally skips several framework features not yet implemented:

- **No Postgres RLS** (Row-Level Security) — 2-layer tenancy (middleware + EF query filter) only.
- **No L2 Redis cache** — `INacCache` default in-memory implementation only.
- **No `IEndpoint` abstraction / `[NacAuthorize]` / `ToMinimalApiResult()`** — uses standard `[ApiController]` + `[Authorize]`.
- **No `dotnet new nac-solution`** template — this sample is the copy-rename blueprint.
- **No `Architecture.Tests`** (NetArchTest) — module-boundary rules enforced by convention.
- **`OutboxWorker` is single-context** — current framework limitation: `NacDbContext` alias resolves to the last registered module context. `AppRootModule` orders `[DependsOn]` so `OrdersModule` registers last → `OrdersDbContext`'s outbox is polled. When the framework exposes `OutboxWorker<TContext>`, remove this ordering dependency.

## 10. Adapting for production

- [ ] Replace `Jwt:SecretKey` with a value from a secret store; rotate regularly.
- [ ] Point `ConnectionStrings:Default` at managed Postgres (pooled, SSL required).
- [ ] Enable L2 cache (Redis via `AddNacCaching` options) once framework supports it.
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production` — disables auto-migrate and admin-seeder.
- [ ] Run migrations from a dedicated job (not in the application startup path).
- [ ] Configure OpenTelemetry exporters in `Nac.Observability`.
- [ ] Remove the `AdminSeeder` default credentials; use an onboarding flow instead.
- [ ] Restrict CORS origins to real domains.
- [ ] Enable HTTPS redirection with a real port in `ASPNETCORE_URLS`.
