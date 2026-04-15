# NAC Framework — Code Standards & Conventions

Guidelines for writing code within the NAC Framework and projects built on it.

---

## Naming Conventions

### Namespaces

**Pattern:** `Nac.{Package}.{Logical.Category}`

```csharp
// ✓ Correct
namespace Nac.Persistence.Repository;
namespace Nac.Messaging.Outbox;
namespace Nac.CQRS.Internal;

// ✗ Incorrect
namespace Persistence;  // Missing Nac prefix
namespace Nac.Persistence.Repositories;  // Plural
```

**Module Projects:** `{Solution}.Modules.{ModuleName}`

```csharp
namespace EShop.Modules.Catalog.Application.Commands;
namespace EShop.Modules.Orders.Domain;
```

### Classes & Records

**PascalCase.** Use **records** for value objects, DTOs, responses; **classes** for entities, services, behaviors.

```csharp
// ✓ Correct
public record CreateProductCommand(string Name, decimal Price) : ICommand<Guid>;
public class EfRepository<TEntity> { }
public class AuthorizationCommandBehavior { }
public sealed record ProductDto(Guid Id, string Name);

// ✗ Incorrect
public record create_product_command() { }  // snake_case
public record CreateProductCommand : ICommand  // unsealed record
public class ProductDTO { }  // Inconsistent casing (DTO not Dto)
```

**Sealed by default** (for records and classes):

```csharp
// ✓ Preferred
public sealed record ProductCreatedDomainEvent(Guid ProductId) : DomainEvent;
public sealed class CatalogModule : INacModule;

// ✗ Only if inheritance needed
public class Product : AggregateRoot<Guid> { }  // Open base class for extension
```

### Methods & Properties

**PascalCase**, concise, verb-driven for methods.

```csharp
// ✓ Correct
public async Task<bool> ExecuteAsync();
public IEnumerable<T> Filter(Specification<T> spec);
public string CacheKey { get; }
public void SetTenantContext(Guid tenantId);

// ✗ Incorrect
public async Task<bool> execute_async();  // snake_case
public IEnumerable<T> GetAll();  // Vague, no specification
public string get_cache_key();  // Method-like property
```

### Parameters

**camelCase**, align with type name.

```csharp
// ✓ Correct
public void Configure(IServiceCollection services, IConfiguration configuration);
public async Task<OrderDto> GetOrderAsync(Guid orderId, CancellationToken cancellationToken);

// ✗ Incorrect
public void Configure(IServiceCollection Services, IConfiguration config);  // PascalCase, abbreviation
public Task<OrderDto> GetOrder(Guid id);  // Too vague
```

### Private Fields

**camelCase with underscore prefix** (or **readonly without prefix** if no mutation).

```csharp
// ✓ Correct
private readonly IMediator _mediator;
private readonly ILogger<CatalogService> _logger;
private int _retryCount;

// ✗ Incorrect
private IMediator mediator;  // No underscore
private static IMediator _mediator;  // Static (use constructor injection instead)
```

### Constants

**UPPER_SNAKE_CASE**:

```csharp
// ✓ Correct
private const int MAX_RETRIES = 3;
private const string DEFAULT_CACHE_KEY_PREFIX = "nac:";
public const string ORDERS_PERMISSION = "orders.manage";

// ✗ Incorrect
private const int maxRetries = 3;
public const string Orders_Manage = "orders.manage";
```

### Interfaces

**I-prefix, PascalCase** (C# convention).

```csharp
// ✓ Correct
public interface IRepository<T> { }
public interface ICacheable { }
public interface ITenantContext { }

// ✗ Incorrect
public interface Repository<T> { }  // No I-prefix
public interface IRepositoryBase { }  // "Base" suffix (use abstract class instead)
```

---

## Language Features (C# 13)

### Records for Immutability

Use **records** (not classes) for DTOs, events, requests, responses:

```csharp
// ✓ Correct
public sealed record CreateProductCommand(string Name, decimal Price, string Description) 
    : ICommand<Guid>;

public sealed record ProductDto(Guid Id, string Name, decimal Price);

public sealed record OutboxMessage(
    Guid Id, 
    string EventType, 
    string EventData, 
    DateTime CreatedAt
);

// ✗ Incorrect
public class CreateProductCommand {  // Class for simple data holder
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

### Primary Constructors

Use **primary constructors** (record or class):

```csharp
// ✓ Correct
public sealed class OrderService(
    IRepository<Order> orderRepository,
    IEventBus eventBus,
    ILogger<OrderService> logger)
{
    public async Task PlaceOrderAsync(CreateOrderCommand command)
    {
        _orderRepository.Add(order);  // Use param name with underscore prefix (auto-field)
    }
}

// ✗ Incorrect
public class OrderService {
    private readonly IRepository<Order> _orderRepository;
    public OrderService(IRepository<Order> orderRepository) 
    {
        _orderRepository = orderRepository;  // Manual field assignment
    }
}
```

### Init Properties

Use **init** for immutable property setting:

```csharp
// ✓ Correct
public class Product : AggregateRoot<Guid>
{
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public string Description { get; init; } = string.Empty;
}

// ✗ Incorrect
public class Product {
    public string Name { get; set; }  // Public setter (mutable)
    public decimal Price { get; set; }
}
```

### Required Members

Enforce initialization:

```csharp
// ✓ Correct
public sealed class CreateProductRequest
{
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public string? Description { get; init; }
}

// ✗ Incorrect
public class CreateProductRequest
{
    public string Name { get; set; }  // Nullable danger
    public decimal Price { get; set; }
}
```

### Nullable Reference Types

**Always enabled.** Use `?` for optional, omit for required.

```csharp
// ✓ Correct
public sealed record ProductDto(
    Guid Id,
    string Name,          // Required
    string? Description   // Optional
);

public async Task<ProductDto?> GetByIdAsync(Guid id);  // Can return null

// ✗ Incorrect
#nullable disable  // Never disable
public string? Name { get; set; }  // Unclear intent
public ProductDto GetByIdAsync(Guid id);  // Null possible but not indicated
```

---

## Pattern: Marker Interfaces & Behaviors

Framework uses **marker interfaces** for opt-in behaviors. Commands/queries declare capability via inheritance.

### Behavioral Markers

```csharp
// ✓ Correct
public sealed record CreateProductCommand(string Name, decimal Price)
    : ICommand<Guid>,
      ITransactional,           // Enable transaction
      IRequirePermission,        // Require auth
      IAuditable                 // Log audit trail
{
    public string Permission => "products.create";
}

public sealed record GetProductsQuery(int Page = 1)
    : IQuery<PaginatedResponse<ProductDto>>,
      ICacheable  // Enable caching
{
    public string CacheKey => $"products:page:{Page}";
    public TimeSpan? Expiry => TimeSpan.FromMinutes(5);
}

// ✗ Incorrect
public record CreateProductCommand(...) : ICommand<Guid>
{
    // No marker interfaces = no behaviors applied
}
```

### Custom Behavior Implementation

```csharp
// Handler implements behavior for a specific command
public sealed class MyCustomBehavior<T> : ICommandBehavior<T> where T : ICommand
{
    private readonly ILogger<MyCustomBehavior<T>> _logger;
    
    public MyCustomBehavior(ILogger<MyCustomBehavior<T>> logger)
    {
        _logger = logger;
    }
    
    public async Task<Unit> HandleAsync(
        T request,
        CommandHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting {CommandName}", typeof(T).Name);
            var result = await next(request, cancellationToken);
            _logger.LogInformation("Completed {CommandName}", typeof(T).Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed {CommandName}", typeof(T).Name);
            throw;
        }
    }
}
```

---

## CQRS Separation

### Commands (Write)

**Always async, always return result or Unit.**

```csharp
// ✓ Correct
public sealed record CreateProductCommand(string Name, decimal Price, string? Description)
    : ICommand<Guid>;  // Returns product ID

public sealed record UpdateProductCommand(Guid Id, string Name, decimal Price)
    : ICommand;  // Returns nothing

// Handler
public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateProductCommand request, CancellationToken ct)
    {
        var product = new Product { Name = request.Name, Price = request.Price };
        _repository.Add(product);
        // UnitOfWork behavior calls SaveChanges — handler must NOT call it
        return product.Id;
    }
}

// ✗ Incorrect
public record CreateProductCommand(...) : ICommand;  // No return type
public record DeleteProductCommand(...);  // Missing interface
```

### Queries (Read)

**Always async, always return specific type (not IEnumerable).**

```csharp
// ✓ Correct
public sealed record GetProductByIdQuery(Guid Id) : IQuery<ProductDto>;

public sealed record SearchProductsQuery(string? Search, int Page = 1)
    : IQuery<PaginatedResponse<ProductDto>>;

public sealed class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto?> HandleAsync(GetProductByIdQuery request, CancellationToken ct)
    {
        var product = await _readRepository.GetByIdAsync(request.Id, ct);
        return product == null ? null : MapToDto(product);
    }
}

// ✗ Incorrect
public record GetAllProductsQuery() : IQuery<IEnumerable<ProductDto>>;  // Too vague
public class GetProductsQueryHandler { }  // Missing IQueryHandler interface
```

### Domain Events (In-process)

**Inherit from DomainEvent, publish from aggregates, handle via INotificationHandler.**

```csharp
// ✓ Correct
public sealed record ProductCreatedDomainEvent(Guid ProductId, string Name) 
    : DomainEvent;

public sealed class ProductCreatedDomainEventHandler 
    : INotificationHandler<ProductCreatedDomainEvent>
{
    public async Task HandleAsync(ProductCreatedDomainEvent notification, CancellationToken ct)
    {
        await _mediator.PublishAsync(new ProductCreatedIntegrationEvent(
            notification.ProductId,
            notification.Name
        ), ct);
    }
}

// ✗ Incorrect
public record ProductCreated : INotification;  // Inheritance broken
public class ProductCreatedHandler { }  // Missing interface
```

---

## Entity & Aggregate Design

### Aggregate Roots

**Inherit from AggregateRoot<TId>, manage domain events, implement optimistic concurrency.**

```csharp
// ✓ Correct
public sealed class Product : AggregateRoot<Guid>
{
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public string Description { get; init; } = string.Empty;
    public bool IsActive { get; private set; }
    
    // Factory method
    public static Product Create(string name, decimal price, string? description = null)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            Description = description ?? string.Empty,
            IsActive = true
        };
        
        product.RaiseDomainEvent(new ProductCreatedDomainEvent(product.Id, name));
        return product;
    }
    
    public void Deactivate()
    {
        IsActive = false;
        RaiseDomainEvent(new ProductDeactivatedDomainEvent(Id));
    }
}

// ✗ Incorrect
public class Product : AggregateRoot<Guid> {
    public string Name { get; set; }  // Mutable property
    public decimal Price { get; set; }
}
```

### Value Objects

**Inherit from ValueObject, immutable, component-based equality.**

```csharp
// ✓ Correct
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    private Money() { }  // EF Core
    
    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentException("Amount must be positive");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency required");
        
        Amount = amount;
        Currency = currency;
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

// ✗ Incorrect
public class Money {
    public decimal Amount { get; set; }  // Mutable
    public string Currency { get; set; }
}
```

---

## Repository & Data Access

### Repository Pattern (No IQueryable)

**Repository returns complete results, never IQueryable. Use Specification for complex queries.**

```csharp
// ✓ Correct
public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Product>> ListByNameAsync(string name, CancellationToken ct = default);
    Task<int> CountActiveAsync(CancellationToken ct = default);
}

// In {Ns}.Modules.Catalog.Infrastructure/Repositories/ProductRepository.cs
public sealed class ProductRepository : EfRepository<Product>, IProductRepository
{
    public ProductRepository(CatalogDbContext context) : base(context) { }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var spec = new GetProductByIdSpec(id);
        return await GetAsync(spec, ct);
    }
}

// ✗ Incorrect
public interface IProductRepository : IRepository<Product>
{
    IQueryable<Product> GetAll();  // Exposed IQueryable
}
```

### Specifications

**Encapsulate query logic in Specification objects.**

```csharp
// ✓ Correct
public sealed class GetProductsByNameSpec : Specification<Product>
{
    public GetProductsByNameSpec(string name)
    {
        Query
            .Where(p => p.Name.Contains(name))
            .OrderBy(p => p.Name)
            .Take(100);
    }
}

public sealed class GetActiveProductsSpec : Specification<Product>
{
    public GetActiveProductsSpec()
    {
        Query.Where(p => p.IsActive);
    }
}

// ✗ Incorrect
var products = _dbContext.Products
    .Where(p => p.Name.Contains(search))
    .ToListAsync();  // Query scattered in handler
```

---

## Dependency Injection & Registration

### Service Registration

**Each module `.Infrastructure` provides a single DI extension. Host calls it with 1 line.**

```csharp
// ✓ Correct — In {Ns}.Modules.Catalog.Infrastructure/CatalogInfrastructureExtensions.cs
public static class CatalogInfrastructureExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddNacPostgreSQL<CatalogDbContext>(connectionString);
        services.AddNacRepositoriesFromAssembly<CatalogDbContext>(
            typeof(CatalogModule).Assembly);
        services.AddScoped<IProductRepository, ProductRepository>();
        return services;
    }
}

// Host (Program.cs) — 1 line per module
services.AddCatalogInfrastructure(connectionString);

// ✗ Incorrect
services.AddScoped<IProductRepository, ProductRepository>();  // In Program.cs directly
services.AddTransient<CatalogService>();  // Transient (should be scoped)
```

---

## Error Handling

### Framework Exceptions

**Throw NacException subtypes. Never throw base Exception.**

```csharp
// ✓ Correct
public async Task<Product> GetProductAsync(Guid id)
{
    var product = await _repository.GetByIdAsync(id);
    if (product == null)
        throw new NotFoundException($"Product {id} not found");
    
    if (!product.IsActive)
        throw new NacException("Product is inactive");
    
    return product;
}

// Handler with permission check
if (!_currentUser.HasPermission("products.view"))
    throw new ForbiddenException("You don't have permission to view products");

// ✗ Incorrect
throw new Exception("Product not found");  // Generic exception
throw new InvalidOperationException(...);  // Not mapped to HTTP status
if (product == null) return null;  // Silent failure
```

---

## Logging

### Structured Logging

**Use ILogger<T> with structured data, not string interpolation.**

```csharp
// ✓ Correct
_logger.LogInformation(
    "Product created: ProductId={ProductId}, Name={ProductName}",
    productId,
    productName);

_logger.LogError(
    ex,
    "Failed to process order: OrderId={OrderId}, UserId={UserId}",
    orderId,
    userId);

// ✗ Incorrect
_logger.LogInformation($"Product created: {productId}");  // No structure
Console.WriteLine("Product created");  // Console output (no context)
```

---

## Testing

### Handler Tests

**Use FakeEventBus, FakeTenantContext, FakeCurrentUser for isolation.**

```csharp
// ✓ Correct
public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesProduct()
    {
        // Arrange
        var handler = new CreateProductCommandHandler(
            _fakeRepository,
            _fakeUnitOfWork);
        
        var command = new CreateProductCommand("Laptop", 1000m);
        
        // Act
        var productId = await handler.HandleAsync(command, CancellationToken.None);
        
        // Assert
        Assert.NotEqual(Guid.Empty, productId);
        var created = await _fakeRepository.GetByIdAsync(productId);
        Assert.NotNull(created);
    }
}

// ✗ Incorrect
var product = await _service.CreateAsync(...);  // No isolation
var dbContext = new TestDbContext();  // Real database
```

---

## File Organization

### Folder Structure (per Module — 2-Project Pattern)

Each module splits into **core** (clean, persistence-ignorant) and **infrastructure** (EF Core).

```
Modules/
  {Ns}.Modules.Catalog/                    ← Core (clean)
    Domain/
      Entities/
        Product.cs              # Aggregate root
        Category.cs
      Events/
        ProductCreatedDomainEvent.cs
      Specifications/
        GetProductByIdSpec.cs
    Application/
      Commands/
        CreateProductCommand.cs
        CreateProductCommandHandler.cs
        CreateProductCommandValidator.cs
      Queries/
        GetProductByIdQuery.cs
        GetProductByIdQueryHandler.cs
      EventHandlers/
        ProductCreatedDomainEventHandler.cs
    Contracts/
      IProductRepository.cs     # Custom repo interface (optional)
    Endpoints/
      ProductEndpoints.cs
    CatalogModule.cs

  {Ns}.Modules.Catalog.Infrastructure/     ← Infrastructure (EF Core)
    CatalogDbContext.cs
    CatalogInfrastructureExtensions.cs
    Configurations/
      ProductConfiguration.cs
    Repositories/
      ProductRepository.cs
```

### File Naming

- **Commands:** `{CommandName}Command.cs` + `{CommandName}CommandHandler.cs` + `{CommandName}CommandValidator.cs`
- **Queries:** `{QueryName}Query.cs` + `{QueryName}QueryHandler.cs`
- **Entities:** `{EntityName}.cs`
- **Specifications:** `{SpecName}Spec.cs` or `Get{EntityName}Spec.cs`
- **Events:** `{EventName}DomainEvent.cs` or `{EventName}IntegrationEvent.cs`
- **DbContext:** `{ModuleName}DbContext.cs` (in `.Infrastructure`)
- **Configurations:** `{EntityName}Configuration.cs` (in `.Infrastructure/Configurations/`)
- **Repositories:** `{EntityName}Repository.cs` (in `.Infrastructure/Repositories/`)
- **DI Extension:** `{ModuleName}InfrastructureExtensions.cs` (in `.Infrastructure`)
- **Contracts:** `I{EntityName}Repository.cs` (in core `Contracts/`)
- **Endpoints:** `{FeatureName}Endpoints.cs` (group by feature, not HTTP verb)

---

## Documentation Comments

### XML Documentation

**Required on public types and members.**

```csharp
// ✓ Correct
/// <summary>
/// Creates a new product with the specified name and price.
/// </summary>
/// <param name="name">Product name (required, max 255 chars)</param>
/// <param name="price">Product price (must be > 0)</param>
/// <param name="description">Optional product description</param>
/// <returns>Created product aggregate root</returns>
/// <exception cref="ArgumentException">Thrown if name is empty or price is negative</exception>
public static Product Create(string name, decimal price, string? description = null)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Name is required", nameof(name));
    // ...
}

// ✗ Incorrect
public static Product Create(string name, decimal price) { }  // No documentation
/// <summary>Create</summary>  // Vague
```

---

## Identity Integration

### Using IIdentityService (Business Modules)

Query user info from Nac.Identity without tight coupling:

```csharp
// In handler (module core)
public sealed class GetStaffByUserIdQueryHandler : IQueryHandler<GetStaffByUserIdQuery, StaffDto?>
{
    private readonly IIdentityService _identityService;
    private readonly IStaffRepository _staffRepository;

    public async Task<StaffDto?> HandleAsync(GetStaffByUserIdQuery query, CancellationToken ct)
    {
        // Get user info from identity service
        var userInfo = await _identityService.GetUserInfoAsync(query.UserId, ct);
        if (userInfo is null)
            return null;

        // Fetch staff by user ID
        var staff = await _staffRepository.GetByUserIdAsync(query.UserId, ct);
        if (staff is null)
            return null;

        return new StaffDto(staff.Id, staff.EmployeeCode, userInfo.DisplayName ?? userInfo.Email);
    }
}
```

### Publishing Identity Events

When `Nac.Messaging` is configured, publish events via `IdentityEventPublisher`:

```csharp
// In identity registration endpoint or command handler
public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Guid>
{
    private readonly UserManager<NacIdentityUser> _userManager;
    private readonly IdentityEventPublisher _eventPublisher;

    public async Task<Guid> HandleAsync(RegisterUserCommand cmd, CancellationToken ct)
    {
        var user = new NacIdentityUser { Email = cmd.Email, UserName = cmd.Email };
        var result = await _userManager.CreateAsync(user, cmd.Password);
        
        if (result.Succeeded)
        {
            // Publish event (safe even if IEventBus not configured)
            await _eventPublisher.PublishUserRegisteredAsync(user, tenantId: null, ct);
            return user.Id;
        }

        throw new InvalidOperationException("Registration failed");
    }
}

// Subscribers listen for events in other modules
public sealed class UserRegisteredIntegrationEventHandler 
    : IIntegrationEventHandler<UserRegisteredEvent>
{
    private readonly IStaffRepository _staffRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task Handle(UserRegisteredEvent evt, CancellationToken ct)
    {
        // Example: auto-create staff record for new user
        var staff = new Staff { UserId = evt.UserId, ... };
        _staffRepository.Add(staff);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

### Linking Business Entities to Users

Use **Guid FK only** (no navigation to NacIdentityUser) to keep modules decoupled from Infrastructure:

```csharp
// ✓ CORRECT — In module core
public sealed class Staff : AggregateRoot<Guid>
{
    public Guid UserId { get; set; }  // FK to NacIdentityUser, no navigation property
    public required string EmployeeCode { get; init; }
    public required string Department { get; set; }
}

// ✓ Query user info via IIdentityService
public async Task<StaffWithUserInfoDto?> GetStaffWithUserAsync(Guid staffId, CancellationToken ct)
{
    var staff = await _repository.GetByIdAsync(staffId, ct);
    if (staff is null)
        return null;

    var userInfo = await _identityService.GetUserInfoAsync(staff.UserId, ct);
    return new StaffWithUserInfoDto(staff.Id, staff.EmployeeCode, userInfo?.Email);
}

// ❌ WRONG — Navigation property couples to Infrastructure
public sealed class Staff : AggregateRoot<Guid>
{
    public NacIdentityUser User { get; set; }  // FORBIDDEN! Couples to Nac.Identity
}
```

---

## Testing & Performance

Comprehensive testing and performance optimization guidelines are covered in **[Testing & Performance](./testing-and-performance.md)** (separate document).

**Key references:**
- Unit testing with Fakes (not Mocks)
- Integration testing with NacTestHost
- N+1 query prevention via Specifications
- Pagination best practices
- Caching rules and invalidation
- Async/await patterns
- Batch operations

---

## Summary: Key Takeaways

1. **Naming:** PascalCase classes/methods, camelCase parameters, UPPER_SNAKE_CASE constants
2. **Records:** DTOs, events, requests—use records (not classes)
3. **Sealed:** Default to sealed classes/records
4. **Init:** Immutable properties use `init`, required members use `required`
5. **Nullable:** Always enabled; use `?` for optional
6. **Markers:** Behavioral capability via marker interfaces
7. **CQRS:** Separate command/query handlers, no mixing
8. **Repositories:** No IQueryable exposure; use Specifications
9. **Exceptions:** Throw NacException subtypes only
10. **Logging:** Structured logging with ILogger<T>, not Console
11. **Testing:** Fakes for unit tests, isolation matters
12. **Async:** All I/O is async; use Task/ValueTask

