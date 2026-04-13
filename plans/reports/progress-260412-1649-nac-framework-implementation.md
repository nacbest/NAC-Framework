# NAC Framework — Implementation Progress

> Updated: 2026-04-12 19:45

## DONE (15/15 packages)

| # | Package | Files | Notes |
|---|---------|-------|-------|
| 1 | **Nac.Abstractions** | 20 | Interfaces, markers, base types |
| 2 | **Nac.Domain** | 9 | Entity, AggregateRoot, ValueObject, DomainEvent |
| 3 | **Nac.Mediator** | 14 | Custom mediator, cmd/query pipelines, behaviors |
| 4 | **Nac.Persistence** | 13 | NacDbContext, EfRepository, UnitOfWork, Outbox+Inbox |
| 5 | **Nac.Persistence.PostgreSQL** | 1 | Npgsql provider |
| 6 | **Nac.Messaging** | 8 | InMemory+Outbox event bus, dispatcher |
| 7 | **Nac.WebApi** | 3 | Response envelope, exception handler |
| 8 | **Nac.Auth** | 2 | Authorization behaviors |
| 9 | **Nac.Caching** | 3 | CachingQueryBehavior, CacheInvalidationBehavior |
| 10 | **Nac.Observability** | 2 | Logging behaviors |
| 11 | **Nac.Testing** | 3 | FakeEventBus, FakeCurrentUser, FakeTenantContext |
| 12 | **Nac.MultiTenancy** | 6 | Resolution middleware, 4 resolvers, tenant store |
| 13 | **Nac.Cli** | 4 | dotnet tool: nac new/add module/feature/entity |
| 14 | **Nac.Templates** | 4 | dotnet new template pack |

| 15 | **Nac.Messaging.RabbitMQ** | 6 | RabbitMQ IEventBus provider (topic exchange, consumer worker) |

**Build:** 0 warnings, 0 errors (15 projects). **~100+ source files, ~2,975 LOC.**

## REMOVED

| Package | Reason |
|---------|--------|
| **Nac.Persistence.SqlServer** | User decision: not needed, PostgreSQL only |
