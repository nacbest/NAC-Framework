using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nac.Abstractions.MultiTenancy;
using Nac.Identity.Data;
using Nac.Identity.Entities;
using NacIdentityOptions = Nac.Identity.Options.NacIdentityOptions;

namespace Nac.Identity.Tests.Fixtures;

/// <summary>
/// Test fixtures for Identity tests.
/// </summary>
public static class TestFixtures
{
    /// <summary>
    /// Creates an in-memory NacIdentityDbContext.
    /// </summary>
    public static NacIdentityDbContext CreateDbContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<NacIdentityDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new NacIdentityDbContext(options);
    }

    /// <summary>
    /// Creates default identity options for testing.
    /// </summary>
    public static IOptions<NacIdentityOptions> CreateOptions(string? signingKey = null)
    {
        return Microsoft.Extensions.Options.Options.Create(new NacIdentityOptions
        {
            SigningKey = signingKey ?? "test-signing-key-must-be-at-least-32-characters-long",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenExpiry = TimeSpan.FromMinutes(15),
            RefreshTokenExpiry = TimeSpan.FromDays(7)
        });
    }

    /// <summary>
    /// Creates a UserManager for testing.
    /// </summary>
    public static UserManager<NacUser> CreateUserManager(NacIdentityDbContext dbContext)
    {
        var store = new FakeUserStore(dbContext);
        return new UserManager<NacUser>(
            store,
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            new PasswordHasher<NacUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<NacUser>>.Instance);
    }

    /// <summary>
    /// Creates a test user.
    /// </summary>
    public static NacUser CreateUser(
        Guid? id = null,
        string? email = null,
        string? userName = null)
    {
        var userId = id ?? Guid.NewGuid();
        return new NacUser
        {
            Id = userId,
            Email = email ?? $"user-{userId}@test.com",
            UserName = userName ?? $"user-{userId}",
            DisplayName = $"Test User {userId}",
            EmailConfirmed = true
        };
    }

    /// <summary>
    /// Creates a tenant role with permissions.
    /// </summary>
    public static TenantRole CreateTenantRole(
        string tenantId,
        string name,
        params string[] permissions)
    {
        return new TenantRole
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Permissions = [.. permissions]
        };
    }

    /// <summary>
    /// Creates a tenant membership.
    /// </summary>
    public static TenantMembership CreateMembership(
        Guid userId,
        string tenantId,
        TenantRole role,
        bool isOwner = false)
    {
        return new TenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            TenantRoleId = role.Id,
            TenantRole = role,
            IsOwner = isOwner
        };
    }
}

/// <summary>
/// Fake HTTP context accessor for testing.
/// </summary>
public sealed class FakeHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; }

    public FakeHttpContextAccessor(ClaimsPrincipal? principal = null)
    {
        if (principal is not null)
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            };
        }
    }

    public static FakeHttpContextAccessor WithUser(
        string userId,
        string? tenantId = null,
        string? name = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId)
        };

        if (name is not null)
            claims.Add(new Claim(ClaimTypes.Name, name));

        if (tenantId is not null)
            claims.Add(new Claim("tenant_id", tenantId));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new FakeHttpContextAccessor(principal);
    }

    public static FakeHttpContextAccessor Anonymous() => new();
}

/// <summary>
/// Fake tenant context for testing.
/// </summary>
public sealed class FakeTenantContext : ITenantContext
{
    public bool IsMultiTenant { get; set; } = true;
    public TenantInfo? Current { get; set; }

    public FakeTenantContext(string? tenantId = null)
    {
        if (tenantId is not null)
        {
            Current = new TenantInfo(tenantId, $"Tenant {tenantId}");
        }
    }
}

/// <summary>
/// Minimal fake user store for testing.
/// </summary>
public sealed class FakeUserStore : IUserStore<NacUser>, IQueryableUserStore<NacUser>
{
    private readonly NacIdentityDbContext _dbContext;

    public FakeUserStore(NacIdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<NacUser> Users => _dbContext.Users;

    public Task<IdentityResult> CreateAsync(NacUser user, CancellationToken ct)
    {
        _dbContext.Users.Add(user);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(NacUser user, CancellationToken ct)
    {
        _dbContext.Users.Remove(user);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<NacUser?> FindByIdAsync(string userId, CancellationToken ct)
    {
        return _dbContext.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId, ct);
    }

    public Task<NacUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct)
    {
        return _dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, ct);
    }

    public Task<string?> GetNormalizedUserNameAsync(NacUser user, CancellationToken ct)
        => Task.FromResult(user.NormalizedUserName);

    public Task<string> GetUserIdAsync(NacUser user, CancellationToken ct)
        => Task.FromResult(user.Id.ToString());

    public Task<string?> GetUserNameAsync(NacUser user, CancellationToken ct)
        => Task.FromResult(user.UserName);

    public Task SetNormalizedUserNameAsync(NacUser user, string? normalizedName, CancellationToken ct)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(NacUser user, string? userName, CancellationToken ct)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> UpdateAsync(NacUser user, CancellationToken ct)
        => Task.FromResult(IdentityResult.Success);

    public void Dispose() { }
}
