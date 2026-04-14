# NAC Framework

[![NuGet](https://img.shields.io/nuget/v/Nac.Abstractions.svg)](https://www.nuget.org/packages/Nac.Abstractions)
[![CI](https://github.com/nacbest/NAC-Framework/actions/workflows/ci.yml/badge.svg)](https://github.com/nacbest/NAC-Framework/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A modular .NET 10 foundation framework for building scalable backend Web APIs with clean architecture principles, opt-in multi-tenancy, and a clear path from monolith to microservices.

**Status:** v1.0 Complete (15/15 packages) | **License:** MIT

---

## Quick Start

### Installation

```bash
# Core packages
dotnet add package Nac.Abstractions
dotnet add package Nac.Domain
dotnet add package Nac.Mediator

# Infrastructure
dotnet add package Nac.WebApi
dotnet add package Nac.Persistence
dotnet add package Nac.Persistence.PostgreSQL

# Optional packages
dotnet add package Nac.Messaging
dotnet add package Nac.Messaging.RabbitMQ
dotnet add package Nac.Caching
dotnet add package Nac.MultiTenancy
dotnet add package Nac.Observability
dotnet add package Nac.Testing

# CLI Tool (for scaffolding)
dotnet tool install -g Nac.Cli
```

### Create New Project

```bash
# Create new project with CLI
nac new MyApp --modules Identity,Catalog,Orders --db postgresql

# Add a feature
cd MyApp
nac add feature Catalog/CreateProduct
```

### Project Structure (Generated)

```
src/
  MyApp.Host/              # Composition root (Program.cs)
  MyApp.Shared/            # Shared contracts, DTOs
  Modules/
    MyApp.Modules.Catalog/
      Domain/              # Entities, aggregates
      Application/         # Commands, queries, handlers
      Infrastructure/      # DbContext, repositories
      Endpoints/          # Minimal API endpoints
      CatalogModule.cs    # Module registration
    MyApp.Modules.Orders/
      ...

tests/
  MyApp.Modules.Catalog.Tests/
  MyApp.Architecture.Tests/

nac.json                   # Framework configuration
MyApp.slnx                 # Solution file
```

---

## Core Concepts

### 1. CQRS with Separate Pipelines

**Commands** (write) and **Queries** (read) have distinct pipelines with different behaviors:

```csharp
// Command: full pipeline (validation → authorization → transaction → handler)
public sealed record CreateProductCommand(string Name, decimal Price) 
    : ICommand<Guid>;

// Query: lightweight pipeline (validation → authorization → cache → handler)
public sealed record GetProductByIdQuery(Guid Id) 
    : IQuery<ProductDto>,
      ICacheable;  // Enable caching
```

### 2. Module-First Architecture

Each module is self-contained with its own DbContext, endpoints, and domain:

```csharp
public sealed class CatalogModule : INacModule
{
    public string Name => "Catalog";
    
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddNacPersistence<CatalogDbContext>(config);
        services.AddNacMediator(x => x.AddHandlers(typeof(CatalogModule).Assembly));
    }
    
    public void ConfigureEndpoints(IEndpointRouteBuilder routes)
    {
        ProductEndpoints.MapProductEndpoints(routes);
    }
}
```

### 3. Opt-in Multi-Tenancy

Enable multi-tenancy only if needed—zero overhead when disabled:

```csharp
builder.AddNacFramework(nac =>
{
    nac.UseMultiTenancy(tenant =>
    {
        tenant.Strategy = TenantStrategy.PerSchema;
        tenant.ResolutionChain = [TenantResolution.Header, TenantResolution.Claim];
    });
});
```

### 4. Dual Event System

**Domain Events** (in-process) + **Integration Events** (distributed):

```csharp
// Domain event: immediate, same transaction
public sealed record OrderCreatedDomainEvent(Guid OrderId) : DomainEvent;

// Integration event: async, eventual consistency
public sealed record OrderCreatedIntegrationEvent(Guid OrderId, decimal Total) 
    : IIntegrationEvent;

// Handler publishes integration event when domain event fires
public sealed class OrderCreatedDomainEventHandler : INotificationHandler<OrderCreatedDomainEvent>
{
    public async Task Handle(OrderCreatedDomainEvent evt, CancellationToken ct)
    {
        await _eventBus.PublishAsync(new OrderCreatedIntegrationEvent(evt.OrderId, ...), ct);
    }
}
```

### 5. Permission-Based Authorization

Flexible permissions with wildcard support:

```csharp
public sealed record CreateProductCommand(...) 
    : ICommand<Guid>,
      IRequirePermission
{
    public string Permission => "catalog.products.create";
}

// Check: user has "catalog.products.create", "catalog.*", or "*.create"
if (!_currentUser.HasPermission("catalog.products.create"))
    throw new ForbiddenException();
```

---

## 15 Packages

| Package | Purpose |
|---------|---------|
| **Nac.Abstractions** | Zero-dependency interfaces (ICommand, IQuery, IRepository, etc.) |
| **Nac.Domain** | Entity, AggregateRoot, ValueObject, DomainEvent |
| **Nac.Mediator** | Custom CQRS mediator (no MediatR dependency) |
| **Nac.Persistence** | EF Core integration, UnitOfWork, Repository, Outbox |
| **Nac.Persistence.PostgreSQL** | PostgreSQL provider wrapper |
| **Nac.Messaging** | IEventBus abstraction, InMemoryEventBus, Outbox pattern |
| **Nac.Messaging.RabbitMQ** | RabbitMQ implementation with consumer worker |
| **Nac.MultiTenancy** | Tenant resolution (Header, Claim, Subdomain, Query), 3 strategies |
| **Nac.Caching** | Query-level caching with post-command invalidation |
| **Nac.Observability** | Structured logging (entry/exit/duration/errors) |
| **Nac.WebApi** | Response envelopes, global exception handler |
| **Nac.Testing** | Fakes (EventBus, TenantContext, CurrentUser) |
| **Nac.Cli** | `nac` CLI tool for scaffolding and management |
| **Nac.Templates** | `dotnet new nac-solution` template |

---

## Key Features

✅ **Custom CQRS Mediator** — Full pipeline control, no MediatR dependency

✅ **Module Isolation** — Clear boundaries, ready for microservice extraction

✅ **Multi-Tenancy** — 3 strategies (Discriminator, Schema-per-tenant, Database-per-tenant)

✅ **Event Bus Abstraction** — Swap between InMemory, RabbitMQ, Kafka (future)

✅ **Repository Pattern** — No IQueryable exposure, Specification pattern for queries

✅ **Distributed Caching** — IDistributedCache abstraction, per-query TTL, invalidation

✅ **Permission-Based Auth** — Flexible wildcard support (module.resource.action)

✅ **Observability** — Structured logging, correlation IDs, OpenTelemetry-ready

✅ **Testing Utilities** — Fakes for isolated unit testing

✅ **CLI-First Workflow** — Scaffold solutions, modules, features with one command

---

## CLI Commands

```bash
# Create new solution
nac new MyApp --modules Identity,Catalog,Orders --db postgresql

# Add module
nac add module Inventory

# Add feature (scaffolds Command, Handler, Validator, Endpoint)
nac add feature Catalog/CreateProduct

# Add entity
nac add entity Catalog/Product

# Add domain event
nac add event Catalog/ProductCreated

# Manage migrations
nac migration add Catalog "InitialCatalog"
nac migration apply

# Verify architecture (no cross-module dependencies)
nac check architecture

# Verify configurations
nac check health

# Update framework packages
nac update
```

---

## Example: Create Product Feature

### 1. Scaffold

```bash
nac add feature Catalog/CreateProduct
```

**Generated:**
- `Application/Commands/CreateProductCommand.cs`
- `Application/Commands/CreateProductCommandHandler.cs`
- `Application/Commands/CreateProductCommandValidator.cs`
- `Endpoints/CreateProductEndpoint.cs`

### 2. Implement

**CreateProductCommand:**
```csharp
public sealed record CreateProductCommand(string Name, decimal Price, string? Description)
    : ICommand<Guid>,
      ITransactional,        // Enable transaction
      IRequirePermission,     // Require permission
      IAuditable             // Enable audit trail
{
    public string Permission => "catalog.products.create";
}
```

**Handler:**
```csharp
public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
{
    private readonly IRepository<Product> _repository;
    
    public CreateProductCommandHandler(IRepository<Product> repository)
    {
        _repository = repository;
    }
    
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = Product.Create(request.Name, request.Price, request.Description);
        _repository.Add(product);
        // Don't call SaveChanges! UnitOfWork behavior handles it
        return product.Id;
    }
}
```

**Endpoint:**
```csharp
public static void MapCreateProductEndpoint(this RouteGroupBuilder group)
{
    group.MapPost("/", CreateProduct)
        .WithName("CreateProduct")
        .Produces<ApiResponse<Guid>>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);
}

private static async Task<IResult> CreateProduct(
    CreateProductRequest request,
    IMediator mediator,
    CancellationToken ct)
{
    var command = new CreateProductCommand(request.Name, request.Price, request.Description);
    var productId = await mediator.Send(command, ct);
    return Results.Created($"/api/catalog/products/{productId}", 
        new ApiResponse<Guid>(productId));
}
```

### 3. Database Migration

```bash
nac migration add Catalog "AddProducts"
nac migration apply
```

### 4. Done

Endpoint is now live at `POST /api/catalog/products` with:
- ✅ Full pipeline (validation, authorization, transaction, audit, caching)
- ✅ Structured logging
- ✅ Exception handling
- ✅ Correlation ID
- ✅ Multi-tenancy support (if enabled)

---

## Architecture Highlights

### Scaling Path

```
PHASE 1: Modular Monolith (Today)
  └─ All modules in single process
     
PHASE 2: Async Messaging (6 months)
  ├─ Add RabbitMQ
  └─ Replace InMemoryEventBus with distributed broker
     
PHASE 3: Microservices (12+ months)
  └─ Extract modules to separate services (zero arch change!)
```

Each module's boundary is already clear—extraction is **mechanical, not architectural**.

### Dependency Graph

```
Nac.Abstractions (zero deps)
  ↑
  ├─ Nac.Domain
  ├─ Nac.Mediator
  ├─ Nac.Persistence
  ├─ Nac.Messaging
  ├─ Nac.MultiTenancy
  ├─ Nac.Caching
  ├─ Nac.Observability
  └─ Nac.WebApi
```

Strictly one-direction. Framework verifies at startup.

---

## Configuration

### Program.cs (Minimal)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNacFramework(nac =>
{
    // Modules
    nac.AddModule<IdentityModule>();
    nac.AddModule<CatalogModule>();
    nac.AddModule<OrdersModule>();
    
    // Persistence
    nac.UsePostgreSql();
    
    // Multi-tenancy (optional)
    nac.UseMultiTenancy(tenant =>
    {
        tenant.Strategy = TenantStrategy.PerSchema;
        tenant.ResolutionChain = [TenantResolution.Header, TenantResolution.Claim];
    });
    
    // Messaging
    nac.UseRabbitMqEventBus(opts =>
    {
        opts.HostName = "rabbitmq";
        opts.ExchangeName = "nac.events";
    });
    
    // Observability
    nac.UseObservability();
    nac.UseAuthorization();
});

var app = builder.Build();
app.UseNacFramework();
app.Run();
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=myapp;Username=postgres;Password=password"
  },
  "RabbitMQ": {
    "HostName": "rabbitmq",
    "Port": 5672
  },
  "Redis": {
    "Connection": "redis:6379"
  }
}
```

### nac.json (Project Manifest)

```json
{
  "framework": { "name": "nac", "version": "1.0.0" },
  "solution": { "name": "MyApp", "namespace": "MyApp" },
  "database": { "provider": "postgresql", "connectionStringKey": "DefaultConnection" },
  "multiTenancy": {
    "enabled": true,
    "strategy": "per-schema",
    "resolution": ["header", "claim"]
  },
  "modules": {
    "Identity": { "version": "1.0.0", "dependencies": [] },
    "Catalog": { "version": "1.0.0", "dependencies": ["Identity"] },
    "Orders": { "version": "1.0.0", "dependencies": ["Identity", "Catalog"] }
  }
}
```

---

## Documentation

- **[Project Overview & PDR](./docs/project-overview-pdr.md)** — Vision, goals, feature matrix
- **[Codebase Summary](./docs/codebase-summary.md)** — Package-by-package breakdown
- **[Code Standards](./docs/code-standards.md)** — Naming, patterns, C# 13 conventions
- **[System Architecture](./docs/system-architecture.md)** — CQRS pipeline, events, multi-tenancy, persistence
- **[Project Roadmap](./docs/project-roadmap.md)** — v1.0 complete, v1.1+ features, adoption targets

---

## Requirements

- **.NET 10+**
- **PostgreSQL 12+** (recommended; others via custom provider)
- **C# 13**

---

## Getting Help

- **Architecture questions:** See [System Architecture](./docs/system-architecture.md)
- **Code examples:** See [Code Standards](./docs/code-standards.md)
- **Implementation guide:** See [Codebase Summary](./docs/codebase-summary.md)
- **Feature roadmap:** See [Project Roadmap](./docs/project-roadmap.md)

---

## Contributing

NAC Framework is actively developed. For:
- **Bug reports:** GitHub Issues
- **Feature requests:** GitHub Discussions
- **Contact:** info@nac.best

---

## License

MIT License — See LICENSE file for details.

---

**© 2026 NAC INFORMATION TECHNOLOGY COMPANY LIMITED**

**Built with ❤️ for clean, scalable .NET architecture.**

