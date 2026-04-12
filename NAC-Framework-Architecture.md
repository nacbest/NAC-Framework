# NAC Framework — Kiến trúc & Giải pháp Hoàn chỉnh

> **Target Runtime:** .NET 10  
> **Architecture:** Modular Clean Architecture + Vertical Slice  
> **Distribution:** Local NuGet Feed + CLI Tool  
> **Multi-tenancy:** Opt-in (3 strategies)

---

## MỤC LỤC

1. [Tầm nhìn tổng thể](#1-tầm-nhìn-tổng-thể)
2. [Kiến trúc tổng quan](#2-kiến-trúc-tổng-quan)
3. [Package Structure](#3-package-structure)
4. [Custom Mediator](#4-custom-mediator)
5. [Persistence Layer](#5-persistence-layer)
6. [Event System — Dual Bus](#6-event-system--dual-bus)
7. [Multi-tenancy](#7-multi-tenancy)
8. [Authentication & Authorization](#8-authentication--authorization)
9. [Module Dependency & Communication](#9-module-dependency--communication)
10. [Cross-cutting Concerns](#10-cross-cutting-concerns)
11. [API Layer](#11-api-layer)
12. [CLI Tool](#12-cli-tool)
13. [Testing Strategy](#13-testing-strategy)
14. [Configuration & Environment](#14-configuration--environment)
15. [Module Registration](#15-module-registration)
16. [Project Structure (Generated)](#16-project-structure-generated)
17. [Development Workflow](#17-development-workflow)
18. [Scalability Path](#18-scalability-path)
19. [Tổng hợp quyết định](#19-tổng-hợp-quyết-định)

---

## 1. Tầm nhìn tổng thể

NAC Framework giải quyết 3 bài toán cốt lõi:

- **Là "foundation" chứ không phải "application"** — cung cấp building blocks (auth, tenancy, persistence, messaging...) mà project cụ thể compose lại theo nhu cầu. Không ép vào kiến trúc cứng nhắc.
- **Multi-tenancy là opt-in** — project nào cần thì bật, không cần thì framework chạy single-tenant mà không có overhead thừa.
- **CLI-first workflow** — developer cài framework trên máy, gõ lệnh là có project mới với đúng các module đã chọn, không copy-paste boilerplate.

---

## 2. Kiến trúc tổng quan

Kết hợp **Clean Architecture** (Uncle Bob) với **Vertical Slice Modularity**.

### Tại sao không Clean Architecture thuần?

Clean Architecture thuần (Application → Domain → Infrastructure tách riêng project) hoạt động tốt cho monolith nhỏ, nhưng khi scale:

- Một thay đổi business logic ở module Orders phải sờ vào 3-4 project
- Cross-cutting concern nhiều, coupling ngầm giữa modules qua shared layer

### Module = Unit of Deployment

Mỗi module tự chứa Clean Architecture riêng (Domain, Application, Infrastructure trong cùng 1 project hoặc tách nếu module lớn). Quyết định tách/gom ở level module, không phải level solution.

**Nguyên tắc:**

- Modules giao tiếp qua **contracts** — không reference trực tiếp nhau
- Module A publish event, Module B subscribe. Shared kernel chỉ chứa integration contracts
- Host chỉ là **composition root** — wire các modules, không chứa business logic

---

## 3. Package Structure

```
Nac.Abstractions            → Interfaces, markers, base types (ZERO dependency)
Nac.Domain                  → Entity, AggregateRoot, ValueObject, DomainEvent base
Nac.Mediator                → Custom mediator, pipeline, behaviors
Nac.Persistence             → EF Core base, UoW, Repository, Outbox
Nac.Persistence.PostgreSQL  → PostgreSQL-specific (provider, migrations helper)
Nac.Persistence.SqlServer   → SQL Server-specific
Nac.MultiTenancy            → Tenant resolution, strategies, provisioning
Nac.Auth                    → Identity integration, JWT, permission system
Nac.Messaging               → IEventBus, InMemoryEventBus, Outbox/Inbox
Nac.Messaging.RabbitMQ      → RabbitMQ implementation
Nac.Messaging.Kafka         → Kafka implementation (future)
Nac.Caching                 → Cache abstraction, behaviors
Nac.Observability           → Logging, metrics, tracing
Nac.WebApi                  → Minimal API helpers, response envelope, versioning
Nac.Testing                 → Test helpers, fakes, architecture rules
Nac.Cli                     → dotnet tool
Nac.Templates               → dotnet new templates
```

### Dependency flow (một chiều, strict):

```
Nac.Abstractions ← không depend gì
    ↑
Nac.Domain ← chỉ depend Abstractions
    ↑
Nac.Mediator ← depend Abstractions
    ↑
Nac.Persistence ← depend Abstractions, Domain, Mediator
    ↑
Nac.MultiTenancy ← depend Abstractions, Persistence
    ↑
Nac.WebApi ← depend Abstractions, Mediator
    ↑
Nac.Auth ← depend Abstractions, Persistence
```

Không package nào depend ngược.

---

## 4. Custom Mediator

### Tại sao không dùng MediatR

Khi build framework, cần kiểm soát 100% pipeline. MediatR không cho control thứ tự resolve behaviors tường minh, không cho inject metadata vào pipeline dễ dàng, và framework không nên phụ thuộc lifecycle third-party package.

### Message Types

Hai loại message tách biệt hoàn toàn:

- **ICommand / ICommand\<TResult\>** — thay đổi state, đi qua full pipeline (validation, authorization, transaction, audit)
- **IQuery\<TResult\>** — read-only, pipeline nhẹ hơn (validation, authorization, caching — không transaction)

Không có class base chung. Đây là thiết kế có chủ đích: nếu chung interface → developer viết behavior apply cho cả hai, phá vỡ CQRS separation.

### Pipeline Behavior Chain

Mediator build chain of behaviors, mỗi behavior quyết định có gọi `next()` hay không. Thứ tự chain phải **deterministic và configurable** tại composition root.

**Command pipeline mặc định:**

```
→ ExceptionHandling
  → Logging
    → Validation
      → Authorization
        → TenantEnrichment (nếu multitenancy enabled)
          → UnitOfWork (auto SaveChanges + dispatch domain events)
            → Handler
```

**Query pipeline mặc định:**

```
→ ExceptionHandling
  → Logging
    → Validation
      → Authorization
        → Caching (nếu query implement ICacheable)
          → Handler
```

### Behavior Registration

Không dùng assembly scanning để tìm behaviors. Developer đăng ký behaviors theo đúng thứ tự mong muốn. Framework cung cấp thứ tự mặc định, module có thể override hoặc insert behavior ở vị trí cụ thể.

### Handler Resolution

Mỗi message type map 1:1 với 1 handler. Framework build dictionary `Type → HandlerFactory` lúc startup. Nếu message không có handler → fail ngay lúc startup (fail-fast principle).

### Pre/Post Processor

Ngoài behaviors (wrap quanh handler), hỗ trợ **pre-processor** và **post-processor** — chạy trước/sau handler nhưng không wrap. Use case: enrichment trước handler, publish notification sau command thành công.

### Notification (In-process event)

Mediator handle **INotification** — one-to-many, fire-and-forget trong process. Domain Events dispatch dưới dạng Notification sau khi UnitOfWork commit thành công.

### Opt-in Behavior qua Marker Interface

```csharp
// Ví dụ
public record GetProductQuery(Guid Id) : IQuery<ProductDto>, ICacheable
{
    public string CacheKey => $"product:{Id}";
    public TimeSpan? Expiry => TimeSpan.FromMinutes(5);
}

public record CreateOrderCommand(...) : ICommand<Guid>, ITransactional, IRequirePermission
{
    public string Permission => "orders.create";
}
```

---

## 5. Persistence Layer

### DbContext per Module (bắt buộc)

Mỗi module sở hữu DbContext riêng. Không có "shared DbContext".

**Lý do:**

- Module boundary rõ ràng — không module nào query trực tiếp table module khác
- Migration độc lập — deploy Catalog không ảnh hưởng migration Orders
- Khi tách microservice, DbContext đã sẵn sàng
- Multi-tenancy apply ở DbContext level, dễ kiểm soát

### Unit of Work

UnitOfWork behavior tự động cho Commands:

1. Behavior mở transaction trước khi gọi handler
2. Handler làm việc với repository, **không** gọi SaveChanges
3. Behavior gọi SaveChanges sau handler return
4. Handler throw exception → transaction rollback
5. Sau SaveChanges thành công → collect domain events từ tracked entities → dispatch qua Mediator Notifications

### Cross-module Transaction

**Nguyên tắc: không có cross-module transaction.** Nếu Orders cần update Inventory → publish Integration Event → Inventory xử lý async. Nếu cần strong consistency → Saga pattern.

**Escape hatch**: TransactionScope bao ngoài cho giai đoạn đầu, nhưng đánh dấu đây không phải best practice. CLI cảnh báo khi detect pattern này.

### Repository Pattern

- **IRepository\<TEntity\>** generic với operations cơ bản
- Repository **không** expose IQueryable ra ngoài
- Trả về domain entities hoặc result set đã hoàn chỉnh
- Query phức tạp → **Specification pattern** — query đóng gói trong Specification object

### Migration Strategy per Module

- Mỗi module có migration folder riêng
- `nac migration add <Module> "<Desc>"` — tạo migration cho module cụ thể
- `nac migration apply` — apply tất cả pending migrations theo dependency order
- `nac migration apply <Module>` — apply cho 1 module

---

## 6. Event System — Dual Bus

### Layer 1: Domain Events (In-process)

Domain Events phát sinh từ domain logic, xử lý trong cùng process, cùng request scope.

- Entity raise domain event → add vào collection trên base Entity class
- UnitOfWork sau SaveChanges → collect domain events → dispatch qua Mediator Notification
- Handlers chạy cùng scope nhưng **sau transaction đã commit**
- Nếu handler cần ghi thêm data → tạo transaction riêng

**Dispatch timing (configurable per event):**

- **Pre-commit**: dispatch trước SaveChanges — handler trong cùng transaction. Nguy hiểm nhưng đôi khi cần.
- **Post-commit** (default): dispatch sau SaveChanges thành công. An toàn hơn.

### Layer 2: Integration Events (Distributed, Async)

Integration Events giao tiếp giữa modules hoặc giữa services. Đây là public contract.

**Abstraction `IEventBus` với 2 implementation:**

- **InMemoryEventBus** — cho development / khi chưa cần broker. Vẫn async (qua background channel) nhưng trong process.
- **Distributed implementations** — RabbitMQ, Kafka... Mỗi cái là NuGet package riêng (`Nac.Messaging.RabbitMQ`).

Swap giữa 2 chỉ cần thay 1 dòng ở composition root. Business logic không biết event đi đâu.

### Outbox Pattern (bắt buộc cho Distributed)

- Integration Event lưu vào bảng `OutboxMessages` trong cùng transaction với business data
- Background worker poll Outbox → publish lên broker → mark processed
- Consumer phía nhận handle **idempotency** (deduplication bằng event ID)

### Inbox Pattern (phía nhận)

Inbox table track event đã xử lý, tránh duplicate processing.

### Phân biệt Domain Event vs Integration Event

| Thuộc tính | Domain Event | Integration Event |
|---|---|---|
| Scope | Trong 1 module | Giữa các modules |
| Transport | In-process (Mediator) | Event Bus (in-mem hoặc broker) |
| Schema ownership | Module nội bộ | Shared Contracts project |
| Serialization | Không cần | Phải serialize (JSON) |
| Versioning | Không cần | Cần (schema evolution) |
| Failure handling | Throw exception | Retry + Dead Letter Queue |

### Flow thực tế:

```
Handler xử lý command
  → Entity raise Domain Event (OrderPlaced)
    → UnitOfWork commit
      → Domain Event Handler chạy in-process
        → Handler publish Integration Event (OrderPlacedIntegrationEvent)
          → Outbox ghi DB (cùng transaction)
            → Background worker publish lên broker
              → Module khác consume
```

---

## 7. Multi-tenancy

### Resolution Pipeline

```
HTTP Request
  → TenantResolutionMiddleware
    → Try Header ("X-Tenant-ID")
    → Try Subdomain (abc.myapp.com → tenant "abc")
    → Try Claim (từ JWT token)
    → Try Route (/api/tenants/{id}/...)
    → Try Query String (?tenant=abc)
    → Fallback: default tenant hoặc reject
  → Set ITenantContext cho request scope
  → Tiếp tục pipeline
```

Compose nhiều resolver theo chain-of-responsibility. Project config resolver nào dùng, thứ tự ưu tiên.

### Data Isolation Strategies

| Strategy | Isolation | Complexity | Use case |
|---|---|---|---|
| **Discriminator** (Column) | Thấp | Đơn giản | SaaS nhỏ, data ít nhạy cảm |
| **Schema-per-tenant** | Trung bình | Vừa | Tách data, 1 DB |
| **Database-per-tenant** | Cao | Cao | Enterprise, compliance |

**Discriminator:** Mỗi table có cột `TenantId`. EF Core global query filter tự động `WHERE TenantId = @current`. Insert tự động set `TenantId`.

**Schema-per-tenant:** Mỗi tenant schema riêng trong cùng database. DbContext tự switch schema theo tenant context.

**Database-per-tenant:** Mỗi tenant database riêng. Connection string resolve từ tenant registry.

### Tenant Registry

Lưu danh sách tenant, configuration, connection string. Nằm ở "host database" (database chung), không thuộc tenant nào.

### Tenant Lifecycle

- **Provisioning**: tạo tenant → tạo schema/database, run migrations, seed data
- **Deactivation**: soft-disable, block access
- **Deletion**: cleanup data (GDPR compliance)

### Khi không dùng Multi-tenancy

- `ITenantContext` vẫn tồn tại nhưng `IsMultiTenant = false`
- Không middleware resolution
- Global query filter không register
- **Zero overhead** — không phải "tenant mặc định", mà là "không có concept tenant"

---

## 8. Authentication & Authorization

### Tách 2 concern

- **Authentication** (bạn là ai): JWT bearer token, cookie, external provider. Wrap ASP.NET Core Identity — module không reference Identity trực tiếp.
- **Authorization** (bạn được làm gì): **Permission-based**, không role-based.

### Permission-based Authorization

- Mỗi module declare danh sách **Permissions** (ví dụ `catalog.products.create`)
- **Role** = tập hợp Permissions, configurable runtime
- Command/Query khai báo permission cần qua marker interface
- Authorization behavior check permission trước handler

**Permission hierarchy:**

```
Module.Resource.Action

catalog.products.create
catalog.products.read
catalog.products.update
catalog.products.delete
catalog.categories.manage    ← wildcard tất cả actions
orders.*                     ← wildcard toàn module
```

### Super Admin & Tenant Admin (khi multitenancy enabled)

- **Host Admin**: quản lý hệ thống, tất cả tenants
- **Tenant Admin**: quản lý trong phạm vi tenant mình
- Permissions scope theo tenant — user A có `orders.create` chỉ trong tenant X

---

## 9. Module Dependency & Communication

### Module Dependency Graph

- Dependency phải **explicit** — khai báo trong module registration
- Dependency **unidirectional** — không circular
- Dependency chỉ qua **Contracts** — interfaces, events, DTOs — không qua implementation

Framework check dependency graph lúc startup. Circular dependency → fail fast.

### 3 cách giao tiếp (theo thứ tự ưu tiên)

**1. Integration Events (preferred):** Async, loose coupling. Module A publish, Module B subscribe. Không biết nhau.

**2. Module Contracts (synchronous query):** Khi cần data ngay (Orders cần giá từ Catalog). Module B expose `ICatalogModuleApi` interface trong Shared Contracts. Implementation trong module B, module A chỉ thấy interface. Đây là anti-corruption layer.

**3. Shared Kernel:** Value objects, enums, types dùng chung. Phải thật ít, thật stable. Ví dụ: `Money`, `Address`, `DateRange`.

### Quy tắc cấm

- Module A **không được** reference project Module B
- Module A **không được** query DbContext Module B
- Module A **không được** gọi internal service Module B
- Cần data → dùng Module Contract hoặc Integration Event

`nac check architecture` verify tất cả quy tắc trên bằng analyze project references và code dependencies.

---

## 10. Cross-cutting Concerns

### Validation

FluentValidation (hoặc self-built). Mỗi Command/Query có validator riêng. Validation behavior chạy trước handler.

- **Input validation** (format, required, range) — trong validator
- **Business validation** (duplicate check, state check) — trong handler/domain

### Caching

- **ICacheable** marker interface trên Query
- Caching behavior check cache trước handler
- Cache invalidation: Command thay đổi data → post-processor invalidate cache keys
- `ICacheInvalidator` interface
- Abstraction: `IDistributedCache` — in-memory, Redis, hoặc bất kỳ

### Observability

- **Structured Logging**: behavior log entry/exit, duration, result. Correlation ID propagate xuyên request.
- **Metrics**: request count, duration, error rate per module per command/query. OpenTelemetry.
- **Distributed Tracing**: mỗi command/query tạo span. Integration events carry trace context.
- **Health Checks**: mỗi module register health check riêng.

### Exception Handling

| Exception | HTTP Status |
|---|---|
| ValidationException | 400 Bad Request |
| UnauthorizedException | 401 |
| ForbiddenException | 403 |
| NotFoundException | 404 |
| ConflictException | 409 |
| DomainException | 422 Unprocessable Entity |
| Unhandled | 500 + correlation ID |

Module throw domain exception, framework translate sang HTTP response. Không leak stack trace.

### Audit Trail

Optional behavior: log mọi command (ai, làm gì, lúc nào, tenant nào, data thay đổi). Ghi audit table hoặc push external service.

---

## 11. API Layer

### Minimal APIs

Dùng Minimal API (không Controllers). Mỗi module group endpoints: `/api/{module-name}/...`

### API Versioning

URL path (`/api/v1/catalog/...`) hoặc header. Module declare version, framework tự route.

### Response Envelope

```json
// Success
{ "data": {...}, "meta": { "timestamp": "...", "traceId": "..." } }

// Error
{ "error": { "code": "...", "message": "...", "details": [...] }, "meta": { "timestamp": "...", "traceId": "..." } }

// Paged
{ "data": [...], "pagination": { "page": 1, "pageSize": 20, "total": 100 }, "meta": {...} }
```

### Rate Limiting

Per-tenant, per-endpoint. Configuration-driven, không hard-code.

---

## 12. CLI Tool

### Cài đặt

```bash
dotnet tool install --global Nac.Cli --add-source /path/to/local/feed
```

### Commands

| Command | Mô tả |
|---|---|
| `nac new <Name>` | Scaffold solution mới |
| `nac add module <Name>` | Thêm module vào solution |
| `nac add feature <Module>/<Feature>` | Tạo Command + Handler + Validator + Endpoint |
| `nac add entity <Module>/<Entity>` | Tạo Entity + Repository interface |
| `nac add event <Module>/<Event>` | Tạo Domain Event + handler skeleton |
| `nac add integration-event <Name>` | Tạo Integration Event trong Shared Contracts |
| `nac migration add <Module> "<Desc>"` | Tạo EF migration cho module |
| `nac migration apply [Module]` | Apply migrations |
| `nac tenant create <Name>` | Provision tenant mới |
| `nac check architecture` | Verify module boundaries |
| `nac check health` | Verify configs, DB connectivity |
| `nac update` | Update framework packages |

### Template Engine

Dùng template files (Scriban hoặc tương tự) trong `Nac.Templates` package. Placeholder cho module name, entity name, namespace.

### nac.json — Project Manifest

```json
{
  "framework": { "name": "nac", "version": "1.0.0" },
  "solution": { "name": "MyApp", "namespace": "MyApp" },
  "database": {
    "provider": "postgresql",
    "connectionStringKey": "DefaultConnection"
  },
  "multiTenancy": {
    "enabled": true,
    "strategy": "per-schema",
    "resolution": ["header", "claim"]
  },
  "modules": {
    "Identity": { "version": "1.0.0", "dependencies": [] },
    "Catalog": { "version": "1.0.0", "dependencies": ["Identity"] },
    "Orders": { "version": "1.0.0", "dependencies": ["Identity", "Catalog"] }
  },
  "messaging": {
    "provider": "in-memory",
    "outbox": true
  }
}
```

---

## 13. Testing Strategy

### Mỗi Module Test Độc Lập

- **Unit Tests**: domain logic, handlers, validators. Mock dependencies.
- **Integration Tests**: handler + real DbContext (Testcontainers). Module có test project riêng.
- **Architecture Tests**: NetArchTest hoặc tương tự, verify dependency rules.

### Framework-provided Test Helpers

- `NacTestHost<TModule>` — isolated test environment cho 1 module
- `FakeEventBus` — capture published events để assert
- `FakeTenantContext` — inject tenant cho test
- `TestMediator` — send command/query qua pipeline trong test

### Architecture Tests Mặc Định

- Module X không reference module Y trực tiếp
- Domain layer không depend Infrastructure
- Handlers không gọi `DbContext.SaveChanges` trực tiếp
- Integration Events nằm trong Shared Contracts

---

## 14. Configuration & Environment

### Layered Configuration

```
appsettings.json                     ← defaults
  → appsettings.{Environment}.json  ← per-environment
    → nac.json                       ← framework config
      → Environment Variables        ← secrets, overrides
        → Tenant-specific config     ← per-tenant overrides (từ DB)
```

### Feature Flags

- Module wrap logic trong feature check
- Flag lưu DB, per-tenant nếu cần
- Không third-party service (nhưng extensible)

---

## 15. Module Registration

```csharp
// Nac.Abstractions
public interface INacModule
{
    string Name { get; }
    void ConfigureServices(IServiceCollection services, IConfiguration config);
    void ConfigureEndpoints(IEndpointRouteBuilder routes);
    void ConfigurePipeline(IApplicationBuilder app) => { } // optional
}

// Program.cs — composition root
var builder = WebApplication.CreateBuilder(args);

builder.AddNacFramework(nac =>
{
    nac.AddModule<IdentityModule>();
    nac.AddModule<CatalogModule>();
    nac.AddModule<OrdersModule>();

    nac.UsePostgreSql();
    nac.UseMultiTenancy(tenant =>
    {
        tenant.Strategy = TenantStrategy.PerSchema;
        tenant.ResolutionStrategy = TenantResolution.Header;
    });

    nac.UseAuthentication();
    nac.UseObservability();
});

var app = builder.Build();
app.UseNacFramework();
app.Run();
```

---

## 16. Project Structure (Generated)

```
MyApp/
├── src/
│   ├── MyApp.Host/                        # Composition root
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── Modules/
│   │   ├── MyApp.Modules.Identity/
│   │   │   ├── Domain/
│   │   │   ├── Application/
│   │   │   │   ├── Commands/
│   │   │   │   ├── Queries/
│   │   │   │   └── EventHandlers/
│   │   │   ├── Infrastructure/
│   │   │   ├── Endpoints/
│   │   │   └── IdentityModule.cs          # INacModule
│   │   │
│   │   ├── MyApp.Modules.Catalog/
│   │   │   ├── Domain/
│   │   │   ├── Application/
│   │   │   ├── Infrastructure/
│   │   │   ├── Endpoints/
│   │   │   └── CatalogModule.cs
│   │   │
│   │   └── MyApp.Modules.Orders/
│   │       └── ...
│   │
│   └── MyApp.Shared/                      # Shared kernel
│       ├── Contracts/                     # Integration events, shared DTOs
│       └── Extensions/
│
├── tests/
│   ├── MyApp.Modules.Catalog.Tests/
│   └── MyApp.Architecture.Tests/
│
└── nac.json
```

---

## 17. Development Workflow

```
1. nac new EShop --modules Identity,Catalog,Orders --db postgresql
   → Solution scaffold xong

2. Viết domain entities trong Modules/Catalog/Domain/

3. nac add feature Catalog/CreateProduct
   → Tạo: CreateProductCommand, Handler, Validator, Endpoint

4. Fill logic vào handler, validator, endpoint

5. nac migration add Catalog "InitialCatalog"

6. nac migration apply

7. dotnet run --project src/EShop.Host

8. nac add module Inventory (khi cần thêm)

9. nac check architecture (verify boundaries)

10. Scale: đổi messaging → RabbitMQ (1 dòng config), bật Outbox, extract module nếu cần
```

---

## 18. Scalability Path

```
Modular Monolith → Modular Monolith + Async Messaging → Microservices
      ↑                        ↑                              ↑
  Bắt đầu ở đây         Thêm RabbitMQ/Kafka          Extract module ra
                         khi cần async                separate service
```

Mỗi module đã có boundary rõ ràng, DbContext riêng, giao tiếp qua events → việc tách microservice là **mechanical**, không phải **architectural rewrite**.

---

## 19. Tổng hợp quyết định

| Quyết định | Lựa chọn | Lý do |
|---|---|---|
| Mediator | Self-built | Full pipeline control, zero third-party dependency |
| ORM | EF Core only | Đủ cho write+read, giảm complexity |
| Event Bus | Dual (in-process + distributed) | Flexible scale path |
| Multi-tenancy | 3 strategies, opt-in | Phù hợp nhiều use case |
| API style | Minimal APIs | Performance, modularity |
| Auth | Permission-based | Flexible hơn role-based |
| Module isolation | Strict — qua contracts only | Enable future extraction |
| Distribution | Local NuGet feed | Offline-first, team share qua private feed |
| CLI | dotnet tool | Native .NET ecosystem |
| Validation | FluentValidation (hoặc self-built) | Tách input/business validation |
| Caching | IDistributedCache abstraction | Swap provider dễ dàng |
| Observability | OpenTelemetry | Industry standard |
| Testing | Per-module + Architecture tests | Đảm bảo boundary integrity |
