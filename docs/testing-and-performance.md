# NAC Framework — Testing & Performance Guidelines

Best practices for writing efficient, well-tested code in the NAC Framework.

---

## Testing Guidelines

### Unit Testing (Handlers)

**Use Fakes, not Mocks.** Nac.Testing provides `FakeEventBus`, `FakeTenantContext`, `FakeCurrentUser`.

```csharp
// ✓ Correct: Fakes from framework
[Fact]
public async Task Handle_ValidCommand_PublishesEvent()
{
    var fakeEventBus = new FakeEventBus();
    var handler = new CreateProductCommandHandler(repository, fakeEventBus);
    
    var result = await handler.Handle(cmd, CancellationToken.None);
    
    var published = fakeEventBus.PublishedOf<ProductCreatedIntegrationEvent>();
    Assert.NotEmpty(published);
}

// ✗ Incorrect: Mocking framework
var eventBusMock = new Mock<IEventBus>();
```

### Integration Testing (E2E)

**Use NacTestHost** for full pipeline testing with real handlers.

- Test command → handlers → database in isolation
- Use in-memory EF Core (or testcontainers for PostgreSQL)
- Verify domain events dispatch, cache invalidation, permissions

### Test Structure

```csharp
public class CreateProductCommandHandlerTests : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly CatalogDbContext _dbContext;
    
    public CreateProductCommandHandlerTests()
    {
        var collection = new ServiceCollection();
        collection.AddNacFramework()
            .AddPersistence(opts => opts.UseInMemoryDatabase("test"))
            .AddMessaging(); // in-memory by default
        _services = collection.BuildServiceProvider();
        _dbContext = _services.GetRequiredService<CatalogDbContext>();
    }
    
    [Fact]
    public async Task CommandRoundTrip_Success()
    {
        var mediator = _services.GetRequiredService<IMediator>();
        var cmd = new CreateProductCommand("Widget", 19.99m);
        
        var productId = await mediator.Send(cmd);
        
        Assert.NotEqual(Guid.Empty, productId);
        var product = await _dbContext.Products.FindAsync(productId);
        Assert.NotNull(product);
    }
    
    public void Dispose() => _dbContext?.Dispose();
}
```

### Test Organization

**File placement:** Tests co-located with feature code or in parallel `tests/` directory.

```
src/
  MyApp.Modules.Catalog/
    Features/
      Products/
        Commands/
          CreateProduct/
            CreateProductCommand.cs
            CreateProductCommandHandler.cs
            CreateProductCommandTests.cs  ← Same folder
```

**Naming:** `{FeatureName}Tests.cs` or `{FeatureName}HandlerTests.cs`

### Mocking Best Practices

- **Avoid Moq/NSubstitute** for framework types (use Fakes)
- **Mock only external services** (email, payment gateways)
- **Test in isolation** — real database (in-memory), real handlers, fake externals

---

## Performance Considerations

### N+1 Query Prevention

**Never use LINQ chains in handlers.** Use `Specification<T>` with `Include` for eager loading.

```csharp
// ✗ Incorrect: N+1 queries
var products = _repository.GetAll();  // Multiple queries if eager-loaded
foreach (var p in products)
    var reviews = _repository.GetReviews(p.Id);  // Query per product!

// ✓ Correct: Single query with spec
var spec = new GetProductsWithReviewsSpec();
var products = await _repository.FindAsync(spec);  // 1 query, eager-loaded
```

### Pagination

**Always paginate on large result sets.** Include `Take(limit).Skip(offset)` in Specification.

```csharp
public sealed record GetProductsQuery(int Page, int PageSize) 
    : IQuery<PaginatedResponse<ProductDto>>;

public sealed class GetProductsQueryHandler 
    : IQueryHandler<GetProductsQuery, PaginatedResponse<ProductDto>>
{
    public async Task<PaginatedResponse<ProductDto>> Handle(GetProductsQuery query, CancellationToken ct)
    {
        var spec = new GetProductsSpec(query.Page, query.PageSize);  // Spec applies Skip/Take
        var (items, total) = await _repository.FindPaginatedAsync(spec, ct);
        return new PaginatedResponse<ProductDto>(items, total, query.Page, query.PageSize);
    }
}
```

**Query string validation:**

```csharp
// ✓ Correct: Bounded limits
const int MaxPageSize = 100;
var pageSize = Math.Min(query.PageSize, MaxPageSize);
var page = Math.Max(1, query.Page);
```

### Caching Rules

- **Commands never cached** — always fresh
- **Queries with ICacheable** — check cache before handler, store after
- **Default TTL:** 5 minutes (configurable per query)
- **Invalidation:** Commands implement `ICacheInvalidator`, specify keys to clear

```csharp
public sealed record GetProductQuery(Guid Id) : IQuery<ProductDto>, ICacheable
{
    public string CacheKey => $"product:{Id}";
    public TimeSpan Expiry => TimeSpan.FromMinutes(5);
}

public sealed record UpdateProductCommand(Guid Id, string Name) 
    : ICommand, ICacheInvalidator
{
    public IEnumerable<string> KeysToInvalidate => new[] { $"product:{Id}", "products:*" };
}
```

### Async/Await Patterns

**Always use async for I/O.** Never block with `.Result` or `.Wait()`.

```csharp
// ✓ Correct
public async Task<Product> GetProductAsync(Guid id)
{
    return await _repository.FindAsync(id);
}

// ✗ Incorrect: Blocking
public Product GetProduct(Guid id)
{
    return _repository.FindAsync(id).Result;  // Thread starvation risk
}
```

### Batch Operations

**For bulk inserts/updates, use `AddRange` or custom batch specs.**

```csharp
// ✓ Correct: Batch insert
var products = new[] { /* 100 products */ };
_repository.AddRange(products);
await _unitOfWork.SaveChangesAsync(ct);

// ✓ Correct: Batch delete (soft)
var spec = new GetInactiveProductsSpec();
var inactive = await _repository.FindAsync(spec);
foreach (var p in inactive)
    p.MarkDeleted();
await _unitOfWork.SaveChangesAsync(ct);
```

### Entity Tracking

**Be explicit about tracking needs:**

```csharp
// ✓ Correct: No-track for read-only
var spec = new GetProductsSpec().AsNoTracking();
var products = await _repository.FindAsync(spec);

// ✓ Correct: Track for mutations
var spec = new GetProductByIdSpec(id);
var product = await _repository.FindAsync(spec);  // tracked
product.UpdatePrice(newPrice);
await _unitOfWork.SaveChangesAsync(ct);
```

---

## Monitoring & Observability

### Logging Best Practices

Use `ILogger<T>` with structured logging:

```csharp
// ✓ Correct
_logger.LogInformation("Product created: {@ProductId}, {@Name}", productId, name);
_logger.LogError(exception, "Failed to process order: {@OrderId}", orderId);

// ✗ Incorrect
Console.WriteLine($"Product created: {productId}");
_logger.LogInformation("Failed to process: " + exception.Message);
```

### Correlation IDs

Framework automatically propagates correlation IDs in observable logging. Reference via:

```csharp
var correlationId = httpContext.TraceIdentifier;
_logger.LogInformation("Processing request {@CorrelationId}", correlationId);
```

---

## Summary: Performance Checklist

- [ ] Queries use Specification with eager loading (no N+1)
- [ ] Large result sets use pagination (max 100 per page)
- [ ] Cached queries implement ICacheable
- [ ] Cache invalidation on related command
- [ ] All I/O is async (no .Result, .Wait())
- [ ] Batch operations for bulk inserts/deletes
- [ ] Explicit `.AsNoTracking()` for read-only queries
- [ ] Structured logging with ILogger<T>
- [ ] Tests use Fakes (FakeEventBus, FakeTenantContext)

