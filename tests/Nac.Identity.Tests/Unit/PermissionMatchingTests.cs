using FluentAssertions;
using Nac.Identity.CurrentUser;
using Nac.Identity.Tests.Fixtures;
using Xunit;

namespace Nac.Identity.Tests.Unit;

public sealed class PermissionMatchingTests
{
    [Theory]
    [InlineData("orders.create", "orders.create", true)]
    [InlineData("orders.*", "orders.create", true)]
    [InlineData("orders.*", "orders.read", true)]
    [InlineData("*.create", "orders.create", true)]
    [InlineData("*.create", "catalog.create", true)]
    [InlineData("*", "anything.here", true)]
    [InlineData("orders.create", "orders.read", false)]
    [InlineData("orders.*", "catalog.create", false)]
    [InlineData("*.create", "orders.read", false)]
    public async Task HasPermission_MatchesCorrectly(
        string grantedPermission,
        string requestedPermission,
        bool expected)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = "test-tenant";

        var dbContext = TestFixtures.CreateDbContext();
        var user = TestFixtures.CreateUser(userId);
        var role = TestFixtures.CreateTenantRole(tenantId, "TestRole", grantedPermission);
        var membership = TestFixtures.CreateMembership(userId, tenantId, role);

        dbContext.Users.Add(user);
        dbContext.TenantRoles.Add(role);
        dbContext.TenantMemberships.Add(membership);
        await dbContext.SaveChangesAsync();

        var httpContext = FakeHttpContextAccessor.WithUser(userId.ToString(), tenantId);
        var tenantContext = new FakeTenantContext(tenantId);

        var currentUser = new JwtCurrentUser(httpContext, tenantContext, dbContext);

        // Act
        var result = currentUser.HasPermission(requestedPermission);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void HasPermission_Unauthenticated_ReturnsFalse()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var httpContext = FakeHttpContextAccessor.Anonymous();
        var tenantContext = new FakeTenantContext("test-tenant");

        var currentUser = new JwtCurrentUser(httpContext, tenantContext, dbContext);

        // Act
        var result = currentUser.HasPermission("any.permission");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasPermission_NoTenant_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dbContext = TestFixtures.CreateDbContext();
        var httpContext = FakeHttpContextAccessor.WithUser(userId.ToString());
        var tenantContext = new FakeTenantContext(); // No tenant

        var currentUser = new JwtCurrentUser(httpContext, tenantContext, dbContext);

        // Act
        var result = currentUser.HasPermission("any.permission");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Permissions_CachedWithinRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = "test-tenant";

        var dbContext = TestFixtures.CreateDbContext();
        var user = TestFixtures.CreateUser(userId);
        var role = TestFixtures.CreateTenantRole(tenantId, "TestRole", "orders.read");
        var membership = TestFixtures.CreateMembership(userId, tenantId, role);

        dbContext.Users.Add(user);
        dbContext.TenantRoles.Add(role);
        dbContext.TenantMemberships.Add(membership);
        await dbContext.SaveChangesAsync();

        var httpContext = FakeHttpContextAccessor.WithUser(userId.ToString(), tenantId);
        var tenantContext = new FakeTenantContext(tenantId);

        var currentUser = new JwtCurrentUser(httpContext, tenantContext, dbContext);

        // Act - access permissions multiple times
        var perms1 = currentUser.Permissions;
        var perms2 = currentUser.Permissions;

        // Assert - should be same instance (cached)
        ReferenceEquals(perms1, perms2).Should().BeTrue();
    }

    [Fact]
    public void HasPermission_EmptyPermission_ReturnsFalse()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var httpContext = FakeHttpContextAccessor.WithUser(Guid.NewGuid().ToString());
        var tenantContext = new FakeTenantContext("test-tenant");

        var currentUser = new JwtCurrentUser(httpContext, tenantContext, dbContext);

        // Act & Assert
        currentUser.HasPermission("").Should().BeFalse();
        currentUser.HasPermission(null!).Should().BeFalse();
    }
}
