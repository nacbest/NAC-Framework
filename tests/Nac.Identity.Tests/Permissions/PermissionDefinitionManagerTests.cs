using FluentAssertions;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Permissions;
using Xunit;

namespace Nac.Identity.Tests.Permissions;

public class PermissionDefinitionManagerTests
{
    [Fact]
    public void Constructor_WithSingleProvider_DiscoverPermissionsFromProvider()
    {
        // Arrange
        var provider = new TestPermissionProvider();

        // Act
        var manager = new PermissionDefinitionManager(new[] { provider });

        // Assert
        manager.GetGroups().Should().HaveCount(1);
        manager.GetAll().Should().HaveCount(3); // Users.Create, Users.Read, Users.Update
    }

    [Fact]
    public void GetOrNull_WithValidPermissionName_ReturnsPermission()
    {
        // Arrange
        var provider = new TestPermissionProvider();
        var manager = new PermissionDefinitionManager(new[] { provider });

        // Act
        var permission = manager.GetOrNull("Users.Create");

        // Assert
        permission.Should().NotBeNull();
        permission!.Name.Should().Be("Users.Create");
    }

    [Fact]
    public void GetOrNull_WithUnknownPermissionName_ReturnsNull()
    {
        // Arrange
        var provider = new TestPermissionProvider();
        var manager = new PermissionDefinitionManager(new[] { provider });

        // Act
        var permission = manager.GetOrNull("NonExistent.Permission");

        // Assert
        permission.Should().BeNull();
    }

    [Fact]
    public void GetAll_ReturnsAllPermissions()
    {
        // Arrange
        var provider = new TestPermissionProvider();
        var manager = new PermissionDefinitionManager(new[] { provider });

        // Act
        var allPermissions = manager.GetAll();

        // Assert
        allPermissions.Should().HaveCount(3);
        allPermissions.Select(p => p.Name).Should().Contain(new[]
        {
            "Users.Create",
            "Users.Read",
            "Users.Update",
        });
    }

    [Fact]
    public void GetGroups_ReturnsAllGroups()
    {
        // Arrange
        var provider = new TestPermissionProvider();
        var manager = new PermissionDefinitionManager(new[] { provider });

        // Act
        var groups = manager.GetGroups();

        // Assert
        groups.Should().HaveCount(1);
        groups[0].Name.Should().Be("Users");
    }

    [Fact]
    public void Constructor_WithMultipleProviders_DiscoverPermissionsFromAllProviders()
    {
        // Arrange
        var provider1 = new TestPermissionProvider();
        var provider2 = new AnotherTestPermissionProvider();

        // Act
        var manager = new PermissionDefinitionManager(new IPermissionDefinitionProvider[] { provider1, provider2 });

        // Assert
        manager.GetGroups().Should().HaveCount(2);
        manager.GetAll().Should().HaveCount(5); // 3 from provider1 + 2 from provider2
    }

    [Fact]
    public void Constructor_WithEmptyProviderList_CreatesEmptyRegistry()
    {
        // Arrange
        var providers = Array.Empty<IPermissionDefinitionProvider>();

        // Act
        var manager = new PermissionDefinitionManager(providers);

        // Assert
        manager.GetGroups().Should().BeEmpty();
        manager.GetAll().Should().BeEmpty();
    }


    [Fact]
    public void Constructor_WithDuplicatePermissionNames_ThrowsArgumentException()
    {
        // Arrange
        var provider1 = new TestPermissionProvider();
        var provider2 = new DuplicateTestPermissionProvider();

        // Act
        var act = () => new PermissionDefinitionManager(new IPermissionDefinitionProvider[] { provider1, provider2 });

        // Assert — duplicate permission names detected with meaningful error
        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate permission*");
    }

    // ── Test Helpers / Mock Providers ─────────────────────────────────────────

    private sealed class TestPermissionProvider : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext context)
        {
            var group = context.AddGroup("Users", "User Management");
            group.AddPermission("Users.Create", "Create User");
            group.AddPermission("Users.Read", "Read User");
            group.AddPermission("Users.Update", "Update User");
        }
    }

    private sealed class AnotherTestPermissionProvider : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext context)
        {
            var group = context.AddGroup("Roles", "Role Management");
            group.AddPermission("Roles.Create", "Create Role");
            group.AddPermission("Roles.Delete", "Delete Role");
        }
    }

    private sealed class HierarchicalTestPermissionProvider : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext context)
        {
            var group = context.AddGroup("Posts", "Post Management");
            var parent = group.AddPermission("Posts.Manage", "Manage Posts");
            parent.AddChild("Posts.Create", "Create Post");
            parent.AddChild("Posts.Delete", "Delete Post");
        }
    }

    private sealed class DuplicateTestPermissionProvider : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext context)
        {
            var group = context.AddGroup("Admin", "Admin Panel");
            group.AddPermission("Users.Create", "Create User (Admin)");
        }
    }
}
