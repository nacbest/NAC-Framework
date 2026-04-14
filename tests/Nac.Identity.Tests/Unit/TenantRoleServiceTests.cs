using FluentAssertions;
using Nac.Identity.Services;
using Nac.Identity.Tests.Fixtures;
using Xunit;

namespace Nac.Identity.Tests.Unit;

public sealed class TenantRoleServiceTests
{
    [Fact]
    public async Task InitializeTenantAsync_CreatesDefaultRoles()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions();
        var service = new TenantRoleService(dbContext, options);
        var user = TestFixtures.CreateUser();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        await service.InitializeTenantAsync("new-tenant", user.Id);

        // Assert
        var roles = await service.GetRolesAsync("new-tenant");
        roles.Should().HaveCount(3);
        roles.Select(r => r.Name).Should().Contain(["Owner", "Admin", "Member"]);
    }

    [Fact]
    public async Task InitializeTenantAsync_AssignsOwnerRole()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions();
        var service = new TenantRoleService(dbContext, options);
        var user = TestFixtures.CreateUser();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        await service.InitializeTenantAsync("new-tenant", user.Id);

        // Assert
        var membership = await service.GetMembershipAsync(user.Id, "new-tenant");
        membership.Should().NotBeNull();
        membership!.IsOwner.Should().BeTrue();
        membership.TenantRole!.Name.Should().Be("Owner");
    }

    [Fact]
    public async Task InitializeTenantAsync_Idempotent()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions();
        var service = new TenantRoleService(dbContext, options);
        var user = TestFixtures.CreateUser();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act - initialize twice
        await service.InitializeTenantAsync("new-tenant", user.Id);
        await service.InitializeTenantAsync("new-tenant", user.Id);

        // Assert - should still have exactly 3 roles
        var roles = await service.GetRolesAsync("new-tenant");
        roles.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateRoleAsync_CreatesCustomRole()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions();
        var service = new TenantRoleService(dbContext, options);

        // Act
        var role = await service.CreateRoleAsync(
            "tenant-1",
            "CustomRole",
            ["custom.read", "custom.write"]);

        // Assert
        role.Should().NotBeNull();
        role.Name.Should().Be("CustomRole");
        role.Permissions.Should().Contain(["custom.read", "custom.write"]);
    }

    [Fact]
    public async Task DeleteRoleAsync_WithMembers_ReturnsFalse()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions();
        var service = new TenantRoleService(dbContext, options);

        var user = TestFixtures.CreateUser();
        var role = TestFixtures.CreateTenantRole("tenant", "CustomRole", "perm1");
        var membership = TestFixtures.CreateMembership(user.Id, "tenant", role);

        dbContext.Users.Add(user);
        dbContext.TenantRoles.Add(role);
        dbContext.TenantMemberships.Add(membership);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await service.DeleteRoleAsync(role.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRoleAsync_NoMembers_ReturnsTrue()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions();
        var service = new TenantRoleService(dbContext, options);

        var role = await service.CreateRoleAsync("tenant", "ToDelete", ["perm1"]);

        // Act
        var result = await service.DeleteRoleAsync(role.Id);

        // Assert
        result.Should().BeTrue();
        var found = await service.GetRoleByNameAsync("tenant", "ToDelete");
        found.Should().BeNull();
    }

    [Fact]
    public async Task AssignUserToTenantAsync_DuplicateMembership_Throws()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions();
        var service = new TenantRoleService(dbContext, options);

        var user = TestFixtures.CreateUser();
        var role = TestFixtures.CreateTenantRole("tenant", "Member", "read");

        dbContext.Users.Add(user);
        dbContext.TenantRoles.Add(role);
        await dbContext.SaveChangesAsync();

        await service.AssignUserToTenantAsync(user.Id, "tenant", role.Id);

        // Act & Assert
        var action = () => service.AssignUserToTenantAsync(user.Id, "tenant", role.Id);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already a member*");
    }

    [Fact]
    public async Task ChangeUserRoleAsync_UpdatesMembership()
    {
        // Arrange
        var dbContext = TestFixtures.CreateDbContext();
        var options = TestFixtures.CreateOptions();
        var service = new TenantRoleService(dbContext, options);

        var user = TestFixtures.CreateUser();
        var role1 = TestFixtures.CreateTenantRole("tenant", "Member", "read");
        var role2 = TestFixtures.CreateTenantRole("tenant", "Admin", "read", "write");

        dbContext.Users.Add(user);
        dbContext.TenantRoles.AddRange(role1, role2);
        await dbContext.SaveChangesAsync();

        await service.AssignUserToTenantAsync(user.Id, "tenant", role1.Id);

        // Act
        await service.ChangeUserRoleAsync(user.Id, "tenant", role2.Id);

        // Assert
        var membership = await service.GetMembershipAsync(user.Id, "tenant");
        membership!.TenantRoleId.Should().Be(role2.Id);
    }
}
