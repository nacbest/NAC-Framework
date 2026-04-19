# NAC Framework — Code Standards & Codebase Structure

## Codebase Organization

### Directory Structure (src/Nac.Core)

```
Nac.Core/
├── Primitives/              # DDD core types
│   ├── Entity.cs            # Base generic entity
│   ├── AggregateRoot.cs     # Aggregate with event sourcing
│   ├── ValueObject.cs       # Immutable value types
│   ├── IDomainEvent.cs      # Domain event marker
│   ├── IStronglyTypedId.cs  # Strongly-typed ID support
│   ├── IAuditableEntity.cs  # Created/Modified tracking
│   └── ISoftDeletable.cs    # Logical deletion marker
│
├── Results/                 # Error handling pattern
│   ├── Result.cs            # Non-generic result
│   ├── ResultT.cs           # Generic Result<T>
│   ├── ResultStatus.cs      # Ok, Invalid, NotFound, etc.
│   └── ValidationError.cs   # Field-level validation
│
├── Domain/                  # Domain services & utilities
│   ├── IRepository.cs       # Write operations contract
│   ├── IReadRepository.cs   # Query operations contract
│   ├── Specification.cs     # Composable query spec (And/Or/Not)
│   ├── Guard.cs             # Input validation utility
│   ├── DomainError.cs       # Domain error marker
│   └── ITenantEntity.cs     # Multi-tenancy marker
│
├── Modularity/              # Module system
│   ├── NacModule.cs         # Base module class
│   ├── DependsOnAttribute.cs  # Module dependency declaration
│   ├── ServiceConfigurationContext.cs  # DI context
│   ├── ApplicationInitializationContext.cs
│   └── ApplicationShutdownContext.cs
│
├── DependencyInjection/     # DI conventions
│   ├── ITransientDependency.cs
│   ├── IScopedDependency.cs
│   ├── ISingletonDependency.cs
│   └── DependencyAttribute.cs
│
├── Abstractions/            # Cross-cutting abstractions
│   ├── Identity/
│   │   ├── ICurrentUser.cs
│   │   ├── IIdentityService.cs
│   │   └── UserInfo.cs
│   ├── Permissions/
│   │   ├── PermissionDefinition.cs
│   │   ├── PermissionGroup.cs
│   │   ├── IPermissionChecker.cs
│   │   ├── IPermissionDefinitionProvider.cs
│   │   └── IPermissionDefinitionContext.cs
│   ├── Events/
│   │   ├── IIntegrationEvent.cs
│   │   ├── UserRegisteredEvent.cs
│   │   ├── UserEmailConfirmedEvent.cs
│   │   └── PasswordResetEvent.cs
│   └── IDateTimeProvider.cs
│
├── DataSeeding/             # Data seeding contracts
│   ├── IDataSeeder.cs
│   └── DataSeedContext.cs
│
├── ValueObjects/            # Pre-built value objects
│   ├── Money.cs
│   ├── Address.cs
│   ├── DateRange.cs
│   └── Pagination.cs
│
└── Nac.Core.csproj
```

### Test Structure (tests/Nac.Core.Tests)

```
Nac.Core.Tests/
├── Primitives/
│   ├── ValueObjectTests.cs
│   ├── EntityTests.cs
│   └── AggregateRootTests.cs
├── Results/
│   ├── ResultTests.cs
│   └── ResultTTests.cs
├── Domain/
│   ├── GuardTests.cs
│   └── SpecificationTests.cs
├── Abstractions/
│   └── Permissions/
│       └── PermissionDefinitionTests.cs
├── ValueObjects/
│   ├── MoneyTests.cs
│   ├── DateRangeTests.cs
│   └── PaginationTests.cs
└── Nac.Core.Tests.csproj
```

---

## Naming Conventions

### Files & Directories
| Element | Convention | Example |
|---------|-----------|---------|
| Namespace | PascalCase | `Nac.Core.Primitives` |
| File | Match class name | `Entity.cs`, `Result.cs` |
| Directory | PascalCase, plural | `Primitives/`, `Results/`, `Abstractions/` |
| Test class | {Class}Tests | `EntityTests.cs` |
| Test method | {Scenario}_{Expected} | `Create_WithValidId_SetsIdProperty()` |

### Code Elements
| Element | Convention | Example |
|---------|-----------|---------|
| Class/Record | PascalCase | `class Entity`, `record UserInfo` |
| Interface | PascalCase, `I` prefix | `interface IRepository`, `ICurrentUser` |
| Method | PascalCase | `public Result Success()` |
| Property | PascalCase | `public bool IsSuccess { get; }` |
| Parameter | camelCase | `public Result(string id)` |
| Local variable | camelCase | `var result = Success();` |
| Constant | UPPER_CASE | `private const string DefaultCurrency = "USD";` |
| Private field | _camelCase | `private readonly List<T> _items;` |

---

## Code Style & Formatting

### C# Language Features
- **Version:** C# 13.0+
- **Nullable Reference Types:** Enabled globally (see Directory.Build.props)
- **Records:** Use for immutable data (e.g., ValueObject, PermissionDefinition)
- **Init-Only Properties:** Use for immutable state initialization
- **Pattern Matching:** Prefer over traditional conditionals
- **LINQ:** Favor over loops for collections

### File Structure Template

```csharp
namespace Nac.Core.{Layer};

/// <summary>
/// Brief description of the type.
/// </summary>
/// <remarks>
/// Detailed behavior, usage notes, or design rationale.
/// </remarks>
public abstract class BaseType
{
    /// <summary>
    /// Property description.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Constructor description.
    /// </summary>
    /// <param name="name">Parameter description.</param>
    public BaseType(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Method description.
    /// </summary>
    /// <returns>Return value description.</returns>
    public abstract void Method();
}
```

### Line Length & Spacing
- **Max Line Length:** 120 characters (soft limit, hard at 150)
- **Blank Lines:** 1 between methods, 2 between logical sections
- **Indentation:** 4 spaces (no tabs)
- **Brace Style:** Allman (opening brace on new line for classes/methods)

### Null Handling Pattern

```csharp
// DO: Explicit null checks with Guard
Guard.NotNull(value, nameof(value));
Guard.NotEmpty(text, nameof(text));

// DO: Nullable reference types
public string? OptionalValue { get; set; }
public required string RequiredValue { get; init; }

// AVOID: Implicit null coalescing
// var x = value ?? default;
```

### Error Handling Pattern

```csharp
// DO: Use Result pattern for business errors
public Result Validate()
{
    if (string.IsNullOrEmpty(Name))
        return Result.Invalid(new ValidationError("Name", "Name is required"));
    
    return Result.Success();
}

// DO: Use exceptions for programming errors
public class Entity<TId>
{
    public TId Id { get; }
    
    public Entity(TId id)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id)); // Programming error
    }
}

// AVOID: Returning null for business logic failures
// if (condition) return null; // Bad pattern
```

---

## Design Patterns & Conventions

### 1. Entity & Aggregate Root

```csharp
// Generic Entity with type-safe ID
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; }
    
    // Domain events
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void RaiseDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
    protected void ClearDomainEvents() => _domainEvents.Clear();
}

// Aggregate root adds transactional boundary
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    // No additional methods; just marks transactional boundary
}
```

### 2. Value Objects

```csharp
// Record-based immutable value object
public abstract record ValueObject
{
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !(left == right);
}

// Concrete value object
public record Money(decimal Amount, string CurrencyCode) : ValueObject
{
    public Money(decimal amount, string currencyCode) 
        : this(
            amount > 0 ? amount : throw new ArgumentException("Amount must be positive"),
            !string.IsNullOrEmpty(currencyCode) ? currencyCode : throw new ArgumentException("Currency required")
        ) { }
}
```

### 3. Result Pattern

```csharp
// Non-generic result
public class Result
{
    public ResultStatus Status { get; }
    public bool IsSuccess => Status == ResultStatus.Ok;
    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    public static Result Success() => new(ResultStatus.Ok);
    public static Result Invalid(params ValidationError[] errors) => new(ResultStatus.Invalid, validationErrors: errors);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
}

// Generic result
public class Result<T> : Result
{
    public T? Value { get; }
    public static Result<T> Success(T value) => new(value, ResultStatus.Ok);
}

// Usage
var result = await _repository.GetAsync(id);
if (!result.IsSuccess)
    return Result.NotFound($"Entity {id} not found");

return Result.Success(result.Value);
```

### 4. Specification Pattern

```csharp
// Composable query specification
public abstract class Specification<T>
{
    protected Specification(Func<T, bool> criteria)
    {
        Criteria = criteria;
    }

    public Func<T, bool> Criteria { get; }

    // Boolean logic operators
    public static Specification<T> operator &(Specification<T> left, Specification<T> right)
        => new CompositeSpecification(t => left.Criteria(t) && right.Criteria(t));

    public static Specification<T> operator |(Specification<T> left, Specification<T> right)
        => new CompositeSpecification(t => left.Criteria(t) || right.Criteria(t));

    public static Specification<T> operator !(Specification<T> spec)
        => new CompositeSpecification(t => !spec.Criteria(t));
}

// Usage
var activeOrAdmins = new ActiveUserSpec() | new AdminUserSpec();
var filtered = users.Where(activeOrAdmins.Criteria).ToList();
```

### 5. Guard Clauses

```csharp
// Input validation utility
public static class Guard
{
    public static void NotNull<T>(T? value, string paramName) where T : class
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
    }

    public static void NotEmpty(string? value, string paramName)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Value cannot be empty", paramName);
    }

    public static void GreaterThanOrEqual(int value, int minimum, string paramName)
    {
        if (value < minimum)
            throw new ArgumentOutOfRangeException(paramName, $"Value must be >= {minimum}");
    }
}

// Usage
public class User
{
    public User(string email, string name)
    {
        Guard.NotEmpty(email, nameof(email));
        Guard.NotEmpty(name, nameof(name));
        
        Email = email;
        Name = name;
    }
}
```

### 6. Module System

```csharp
// Module dependency declaration
[DependsOn(typeof(CoreModule))]
public class ApplicationModule : NacModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        
        // Register module services
        services.AddScoped<IMyService, MyService>();
    }

    public override Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        // Initialization logic (seed data, warming caches, etc.)
        return Task.CompletedTask;
    }
}

// Usage: Dependency injection framework auto-resolves and wires modules
```

---

## Documentation Standards

### XML Comments
- **Required For:** All public types, methods, and properties
- **Format:** Follow standard C# XML comment syntax
- **Content:** Brief description + detailed remarks for complex behavior

```csharp
/// <summary>
/// Represents a domain entity with a unique identifier.
/// </summary>
/// <typeparam name="TId">The type of the entity's ID.</typeparam>
/// <remarks>
/// This base class provides common entity behavior including domain event tracking.
/// Entities are reference types identified by their ID, not their property values.
/// </remarks>
public abstract class Entity<TId> where TId : notnull
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public TId Id { get; protected set; }

    /// <summary>
    /// Raises a domain event to be processed by event handlers.
    /// </summary>
    /// <param name="event">The domain event to raise.</param>
    /// <remarks>
    /// Domain events are stored and cleared after being published.
    /// </remarks>
    protected void RaiseDomainEvent(IDomainEvent @event);
}
```

### Inline Comments
- **Keep minimal:** Code should be self-documenting
- **When needed:** Explain "why," not "what"
- **Mark special cases:** TODOs, workarounds, known issues

```csharp
// Good: Explains the why
// ReSharper disable once UseObjectInitializer
var entity = new Entity<int>(1);
entity.Name = "Test"; // Must use property setter for event tracking

// Avoid: Explains the obvious
var count = items.Count; // Get the count of items
```

---

## Testing Standards

### Test Organization
- **Framework:** xUnit.v3 (v3.2.2+)
- **Assertions:** FluentAssertions
- **Naming:** `{MethodName}_{Scenario}_{Expected}` or `{MethodName}When{Condition}Then{Expected}`
- **Structure:** Arrange-Act-Assert (AAA)

### xUnit v3 Requirements
Test projects must declare `OutputType=Exe` in the project file (xUnit v3 requirement):
```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <IsPackable>false</IsPackable>
  <IsTestProject>true</IsTestProject>
</PropertyGroup>
```

For suppressing xUnit analyzer warnings (e.g., CancellationToken usage in unit tests):
```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);xUnit1051</NoWarn>
</PropertyGroup>
```

### Test File Template

```csharp
namespace Nac.Core.Tests.Primitives;

using FluentAssertions;
using Xunit;
using Nac.Core.Primitives;

public class EntityTests
{
    [Fact]
    public void Create_WithValidId_SetsIdProperty()
    {
        // Arrange
        var expectedId = 1;
        
        // Act
        var entity = new TestEntity(expectedId);
        
        // Assert
        entity.Id.Should().Be(expectedId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_WithInvalidName_ThrowsArgumentException(string name)
    {
        // Arrange & Act & Assert
        var action = () => new TestEntity(1) { Name = name };
        action.Should().Throw<ArgumentException>();
    }
    
    // Private helper class for testing abstract Entity
    private class TestEntity : Entity<int>
    {
        public string? Name { get; set; }
        
        public TestEntity(int id)
        {
            Id = id;
        }
    }
}
```

### Coverage Goals
- **Target:** 80%+ line coverage
- **Focus:** Happy path + edge cases
- **Ignore:** Generated code, trivial getters

---

## Performance Considerations

### Memory Efficiency
- **Value objects:** Immutable, stack-allocated when possible
- **Collections:** Use IEnumerable<T> for lazy evaluation
- **Large results:** Stream or paginate

### Execution Speed
- **Specifications:** Pre-compile expensive predicates
- **Repositories:** Batch operations; use bulk insert
- **Async:** Use async/await for I/O-bound operations

### Benchmarking
- Run benchmarks before optimizing
- Document assumptions in code
- Include performance regressions in CI

---

## Dependency Management

### External Dependencies (L0 Only)
```xml
<ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" />
</ItemGroup>
```

- **Only abstractions:** No implementations
- **No business logic dependencies:** All core logic is custom
- **Version management:** Centralized in Directory.Packages.props

---

## Security Standards

### Input Validation
- Validate all public inputs (Guard clauses)
- Reject null for non-nullable parameters
- Bounds-check numeric values

### Data Protection
- Immutable entities and value objects
- No sensitive data in logs
- Encrypt at rest (L2+ responsibility)

### Example
```csharp
public class User
{
    public string Email { get; }
    
    public User(string email)
    {
        Guard.NotEmpty(email, nameof(email));
        
        // Validate email format (basic check)
        if (!email.Contains("@"))
            throw new ArgumentException("Invalid email format");
        
        Email = email;
    }
}
```

---

## Versioning & Compatibility

### Breaking Changes
- Mark deprecated APIs with `[Obsolete]`
- Major version bump for breaking changes
- Provide migration guide in release notes

### Example
```csharp
[Obsolete("Use NewMethod() instead. Will be removed in v2.0.0.", false)]
public void OldMethod()
{
    NewMethod();
}

public void NewMethod()
{
    // Improved implementation
}
```

---

## Build & Compilation

### Directory.Build.props (Shared Settings)
```xml
<PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TargetFramework>net10.0</TargetFramework>
    <NacFrameworkVersion>1.0.0</NacFrameworkVersion>
</PropertyGroup>
```

### Directory.Packages.props (Centralized Versions)
```xml
<ItemGroup>
    <!-- BCL Abstractions -->
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.6" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.6" />
    
    <!-- CQRS & Caching -->
    <PackageVersion Include="FluentValidation" Version="12.1.1" />
    <PackageVersion Include="Microsoft.Extensions.Caching.Hybrid" Version="10.5.0" />
    
    <!-- Persistence -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.6" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.6" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.6" />
    
    <!-- Identity & JWT (Wave 2B) -->
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.6" />
    <PackageVersion Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.6" />
    <PackageVersion Include="Microsoft.IdentityModel.Tokens" Version="8.0.1" />
    <PackageVersion Include="System.IdentityModel.Tokens.Jwt" Version="8.0.1" />
    
    <!-- Event Bus -->
    <PackageVersion Include="System.Threading.Channels" Version="8.0.0" />
    
    <!-- Testing -->
    <PackageVersion Include="xunit.v3" Version="3.2.2" />
    <PackageVersion Include="FluentAssertions" Version="8.9.0" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
</ItemGroup>
```

---

## Multi-Tenancy Patterns (Wave 2B)

### Multi-Tenant Entity Pattern

```csharp
// Entities implementing ITenantEntity are automatically filtered
public class Customer : Entity<CustomerId>, ITenantEntity
{
    public string TenantId { get; set; } = default!;
    public string Name { get; set; }
    
    // DbContext automatically filters: .Where(c => c.TenantId == currentTenantId)
}
```

### Tenant Resolution Pattern

```csharp
// Middleware resolves tenant from multiple sources
app.UseNacMultiTenancy()
    .AddHeaderTenantStrategy("X-Tenant-Id")
    .AddClaimTenantStrategy("tenant_id")
    .AddRouteTenantStrategy("tenantId");

// First matching strategy wins
// Usage in controller: [Route("/api/{tenantId}/customers")]
```

### Per-Tenant Database Pattern

```csharp
// Register custom connection string resolver
services.AddScoped<ITenantConnectionStringResolver, MyTenantConnectionStringResolver>();

public class MyTenantConnectionStringResolver : ITenantConnectionStringResolver
{
    public string Resolve(string tenantId)
    {
        // Return tenant-specific connection string
        // E.g., "Server=db-tenant-{tenantId};Database=AppDb"
    }
}
```

---

## Identity & Authorization Patterns (Wave 2B)

### Permission Definition Pattern

```csharp
// Define permissions as IPermissionDefinitionProvider
public class MyPermissions : IPermissionDefinitionProvider
{
    public void Define(IPermissionDefinitionContext context)
    {
        var users = context.AddGroup("Users");
        users.AddPermission("Create", displayName: "Create Users");
        users.AddPermission("Edit", displayName: "Edit Users");
        users.AddPermission("Delete", displayName: "Delete Users", isGrantedByDefault: false);
        
        var reports = context.AddGroup("Reports");
        reports.AddPermission("View");
        reports.AddPermission("Export", isGrantedByDefault: false);
    }
}

// Register in DI
services.AddNacIdentity<MyDbContext>()
    .AddPermissionDefinitionProvider<MyPermissions>();
```

### Permission-Based Authorization Pattern

```csharp
// In controller: require specific permission
[Authorize(Policy = "Users.Delete")]
public async Task<IActionResult> DeleteUser(Guid id)
{
    // Only users with "Users.Delete" permission
    return Ok();
}

// Programmatic check
if (await _permissionChecker.IsGrantedAsync("Reports.Export"))
{
    // User can export reports
}
```

### JWT Token Claim Pattern

```csharp
// JwtTokenService adds standard claims
var token = await _jwtTokenService.GenerateAsync(user);

// Token includes:
// - sub: user.Id
// - name: user.FullName
// - email: user.Email
// - tenant_id: user.TenantId
// - role: user roles (repeating claim)
```

### User Info Retrieval Pattern

```csharp
// IdentityService provides user queries
var userInfo = await _identityService.GetUserInfoAsync(userId);
// Returns: UserInfo(Id, Email, FullName, TenantId, Roles)

var users = await _identityService.GetUsersAsync(userIds);
// Bulk user info retrieval
```

---

## Checklist Before Commit

- [ ] Code compiles without warnings
- [ ] All tests pass (no skipped tests)
- [ ] New public APIs have XML comments
- [ ] No TODO/FIXME comments without context
- [ ] File naming follows conventions
- [ ] Namespace matches directory structure
- [ ] No hardcoded values; use constants
- [ ] Guard clauses for all public inputs
- [ ] Result pattern used for business errors
- [ ] Multi-tenant entities implement ITenantEntity
- [ ] Permissions follow hierarchy and naming conventions

---

**Last Updated:** 2026-04-16 (Wave 2B)
**C# Version:** 13.0+  
**Target Framework:** .NET 10.0 LTS
