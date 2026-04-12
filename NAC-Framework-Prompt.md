# PROMPT: Build NAC Framework (.NET 10)

> Copy toàn bộ nội dung bên dưới và gửi cho AI kèm file `NAC-Framework-Architecture.md` đính kèm.

---

## CONTEXT

Tôi đang xây dựng **NAC Framework** — một modular .NET 10 framework dùng làm foundation cho các project backend (Web API). Framework phải hỗ trợ multitenant (opt-in) và được phân phối qua local NuGet feed + CLI tool.

File đính kèm `NAC-Framework-Architecture.md` chứa **toàn bộ kiến trúc và giải pháp chi tiết** đã được thảo luận và chốt. Hãy đọc kỹ file này trước khi bắt đầu — đây là source of truth cho mọi quyết định thiết kế.

---

## YÊU CẦU TỔNG QUAN

Hãy implement NAC Framework theo đúng kiến trúc trong file đính kèm. Framework bao gồm các NuGet packages sau (theo thứ tự dependency):

1. **Nac.Abstractions** — Interfaces, marker interfaces, base types. Zero external dependency.
2. **Nac.Domain** — Entity, AggregateRoot, ValueObject, DomainEvent base classes.
3. **Nac.Mediator** — Custom mediator (KHÔNG dùng MediatR), pipeline behaviors, handler resolution.
4. **Nac.Persistence** — EF Core base, UnitOfWork behavior, Repository pattern, Outbox table.
5. **Nac.Persistence.PostgreSQL** — PostgreSQL provider, migration helpers.
6. **Nac.Persistence.SqlServer** — SQL Server provider, migration helpers.
7. **Nac.MultiTenancy** — Tenant resolution middleware, 3 strategies (Discriminator, Schema, Database), tenant registry, provisioning.
8. **Nac.Auth** — ASP.NET Core Identity wrapper, JWT, permission-based authorization system.
9. **Nac.Messaging** — IEventBus abstraction, InMemoryEventBus, Outbox/Inbox pattern infrastructure.
10. **Nac.Messaging.RabbitMQ** — RabbitMQ implementation của IEventBus.
11. **Nac.Caching** — Cache abstraction, ICacheable, CachingBehavior, ICacheInvalidator.
12. **Nac.Observability** — Structured logging, OpenTelemetry metrics/tracing, health checks.
13. **Nac.WebApi** — Minimal API helpers, response envelope (Success/Error/Paged), API versioning, rate limiting, global exception handler.
14. **Nac.Testing** — NacTestHost\<TModule\>, FakeEventBus, FakeTenantContext, TestMediator, architecture test rules.
15. **Nac.Cli** — dotnet tool với commands: new, add module, add feature, add entity, add event, migration, tenant, check architecture, check health, update.
16. **Nac.Templates** — dotnet new templates dùng bởi CLI.

---

## QUY TẮC KỸ THUẬT BẮT BUỘC

### General
- Target: .NET 10 (net10.0)
- Language: C# 13, enable nullable, implicit usings
- Mỗi package là 1 project trong cùng solution `Nac.sln`
- Dependency flow phải strict một chiều (xem Architecture doc)
- Không dùng MediatR, Autofac, hoặc bất kỳ DI container nào ngoài Microsoft.Extensions.DependencyInjection
- Dùng `sealed` class khi không cần inheritance
- Dùng `record` cho Commands, Queries, Events, DTOs
- Dùng `readonly struct` cho lightweight Value Objects khi phù hợp

### Custom Mediator (Nac.Mediator)
- ICommand / ICommand\<TResult\> và IQuery\<TResult\> phải tách biệt hoàn toàn, KHÔNG có base interface chung
- Pipeline behavior chain: mỗi behavior nhận `next` delegate, quyết định gọi hay không
- Handler resolution: build dictionary Type → HandlerFactory lúc startup, fail-fast nếu thiếu handler
- Behavior registration theo thứ tự explicit, không assembly scanning cho behaviors
- Hỗ trợ INotification (one-to-many, in-process)
- Hỗ trợ Pre/Post processors

### Persistence (Nac.Persistence)
- DbContext per module — không shared DbContext
- UnitOfWork behavior: auto transaction + SaveChanges + domain event dispatch
- Domain events dispatch POST-commit by default (configurable pre-commit per event)
- Repository không expose IQueryable — trả về entities hoặc completed result sets
- Specification pattern cho complex queries
- Outbox table cho integration events

### Event System
- Domain Events: in-process qua Mediator INotification, dispatch sau UoW commit
- Integration Events: qua IEventBus — InMemoryEventBus (default) hoặc distributed (RabbitMQ)
- Outbox pattern: event ghi vào OutboxMessages table cùng transaction → background worker publish
- Inbox pattern: consumer side deduplication
- Domain Event và Integration Event PHẢI là types tách biệt

### Multi-tenancy (Nac.MultiTenancy)
- 3 strategies: Discriminator (column TenantId), Schema-per-tenant, Database-per-tenant
- Tenant resolution middleware: chain-of-responsibility (Header, Subdomain, Claim, Route, QueryString)
- ITenantContext injectable, IsMultiTenant = false khi không bật → zero overhead
- Tenant Registry ở host database
- Tenant provisioning pipeline (tạo schema/DB, run migrations, seed)

### Auth (Nac.Auth)
- Wrap ASP.NET Core Identity — modules không reference Identity trực tiếp
- Permission-based: modules declare permissions, roles = tập hợp permissions (runtime configurable)
- Permission format: `module.resource.action` — hỗ trợ wildcard (`orders.*`)
- Authorization behavior check permission trước handler
- Host Admin / Tenant Admin khi multitenancy enabled

### WebApi (Nac.WebApi)
- Minimal APIs only, không Controllers
- Response envelope: `{ data, meta }` / `{ error: { code, message, details }, meta }` / `{ data, pagination, meta }`
- Global exception handler: map exception types → HTTP status codes
- API versioning (URL path hoặc header)
- Per-tenant, per-endpoint rate limiting

### CLI (Nac.Cli)
- Dùng System.CommandLine
- Đọc/ghi nac.json (project manifest)
- Template engine dùng Scriban
- Tất cả commands phải idempotent khi có thể

### Testing (Nac.Testing)
- NacTestHost\<TModule\>: isolated environment cho 1 module
- FakeEventBus: capture events để assert
- FakeTenantContext: inject tenant context
- Ship sẵn architecture test rules (NetArchTest)

---

## CÁCH LÀM VIỆC

1. **Đọc kỹ file Architecture đính kèm** — hiểu toàn bộ thiết kế trước khi code.
2. **Bắt đầu từ packages nền tảng** — Nac.Abstractions → Nac.Domain → Nac.Mediator (theo dependency order).
3. **Mỗi package phải**:
   - Có đủ public API (interfaces, classes, extension methods)
   - Có ServiceCollection extension method để register (`services.AddNacMediator()`, `services.AddNacPersistence()`, v.v.)
   - Có XML documentation cho public members
   - Không violate dependency direction
4. **Composition root** (trong generated project) phải sạch — chỉ gọi `builder.AddNacFramework(...)` và `app.UseNacFramework()`.
5. **Viết code production-ready** — không placeholder, không TODO, không stub. Mỗi component phải hoạt động thực sự.

---

## OUTPUT FORMAT

Với mỗi package, hãy output:
- Project file (.csproj) với đúng dependencies
- Tất cả source files với full implementation
- README ngắn cho package đó (cách dùng, cách register)

Hãy bắt đầu với **Nac.Abstractions**, **Nac.Domain**, và **Nac.Mediator** trước (3 packages nền tảng). Sau khi hoàn thành, tôi sẽ yêu cầu tiếp các packages tiếp theo.

---

## LƯU Ý QUAN TRỌNG

- Đây là FRAMEWORK, không phải application. Code phải generic, extensible, không chứa business logic cụ thể.
- Mọi quyết định thiết kế đã được chốt trong file Architecture. Không thay đổi architecture — chỉ implement.
- Nếu có ambiguity trong architecture doc, hãy hỏi trước khi implement.
- Quality > Speed. Code phải clean, consistent, well-structured.
