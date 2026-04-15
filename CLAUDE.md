# NAC Framework

> Modular .NET 10 framework with CQRS, multi-tenancy, and clean architecture.
> Complete API reference & usage patterns: **see `llms-full.txt`**

## Project Structure

```
src/
├── Nac.Core/                   — [L0] Contracts (zero ASP.NET Core deps)
├── Nac.Domain/                 — Domain primitives, persistence interfaces
├── Nac.CQRS/                   — Custom mediator, pipeline behaviors
├── Nac.Persistence/            — EF Core: NacDbContext, repositories, UoW
├── Nac.Persistence.PostgreSQL/ — PostgreSQL provider
├── Nac.Identity/               — ASP.NET Identity + JWT + tenant permissions
├── Nac.MultiTenancy/           — Tenant resolution, strategies
├── Nac.Caching/                — Query caching behaviors
├── Nac.Messaging/              — Event bus (InMemory, Outbox)
├── Nac.Messaging.RabbitMQ/     — RabbitMQ implementation
├── Nac.Observability/          — Structured logging behaviors
├── Nac.WebApi/                 — Response envelopes, exception handler, module framework
├── Nac.Testing/                — Test fakes
├── Nac.Templates/              — dotnet new templates
└── Nac.Cli/                    — dotnet global tool (`nac` command), under /src/Tooling/
```

## Package Dependency Rules (CRITICAL)

```
L0: Nac.Core (zero deps)
     ↑
L1:  Nac.Domain, Nac.CQRS, Nac.Caching
     ↑
L2+: Nac.Persistence → Nac.Persistence.PostgreSQL
     Nac.Identity, Nac.Messaging → Nac.Messaging.RabbitMQ
     Nac.MultiTenancy, Nac.Observability, Nac.WebApi, Nac.Testing
```

```
Module (core) → Nac.Core, Nac.Domain, Nac.CQRS ONLY
Module.Infrastructure → Nac.Persistence + Module (core)
Host → Module.Infrastructure + Module (core) + Nac.Identity
```

## Forbidden Patterns

- Module core → Nac.Persistence ✗
- Module core → Nac.Identity ✗
- Module core → own .Infrastructure ✗
- Navigation property to NacIdentityUser ✗ (use `Guid UserId`)
- Cross-module DbContext access ✗
- Direct project references between modules ✗
- IQueryable exposure from repositories ✗
- Handlers calling SaveChanges ✗

## Key Conventions

- **Identity linking**: Business entities use `Guid UserId` — no navigation property. FK at DB level only.
- **Module pattern**: 2-project split — core (clean) + infrastructure (EF Core).
- **CQRS handlers**: Never call SaveChanges — UnitOfWorkBehavior handles it.
- **Repositories**: Inject `IRepository<T>` / `IReadRepository<T>`, never DbContext in module core.
- **Custom queries**: Interface in module core `Contracts/`, implementation in `.Infrastructure`.
- **Permissions**: Format `module.resource.action` with wildcard support.
- **Pipeline order (Command)**: Observability → Authorization → CacheInvalidation → UnitOfWork → Handler
- **Pipeline order (Query)**: Observability → Authorization → Caching → Handler
