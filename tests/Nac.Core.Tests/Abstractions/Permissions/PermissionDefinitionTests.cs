using FluentAssertions;
using Nac.Core.Abstractions.Permissions;
using Xunit;

namespace Nac.Core.Tests.Abstractions.Permissions;

public class PermissionDefinitionTests
{
    private static PermissionGroup CreateTestGroup(string name, string? displayName = null)
    {
        // Using reflection to access internal constructor
        var ctor = typeof(PermissionGroup).GetConstructors(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)[0];
        return (PermissionGroup)ctor.Invoke(new object?[] { name, displayName })!;
    }

    [Fact]
    public void AddPermission_CreatesPermissionDefinition()
    {
        // Arrange
        var permissionGroup = CreateTestGroup("TestGroup");

        // Act
        var permission = permissionGroup.AddPermission("TestPermission");

        // Assert
        permission.Should().NotBeNull();
        permission.Name.Should().Be("TestPermission");
        permission.DisplayName.Should().BeNull();
    }

    [Fact]
    public void AddPermission_WithDisplayName_SetsDisplayName()
    {
        // Arrange
        var permissionGroup = CreateTestGroup("TestGroup");

        // Act
        var permission = permissionGroup.AddPermission("TestPermission", "Test Permission Display");

        // Assert
        permission.DisplayName.Should().Be("Test Permission Display");
    }

    [Fact]
    public void PermissionGroup_Permissions_ContainsAddedPermissions()
    {
        // Arrange
        var permissionGroup = CreateTestGroup("TestGroup");
        var perm1 = permissionGroup.AddPermission("Permission1");
        var perm2 = permissionGroup.AddPermission("Permission2");

        // Act & Assert
        permissionGroup.Permissions.Should().HaveCount(2);
        permissionGroup.Permissions.Should().Contain(perm1);
        permissionGroup.Permissions.Should().Contain(perm2);
    }

    [Fact]
    public void AddChild_CreatesChildPermission()
    {
        // Arrange
        var permissionGroup = CreateTestGroup("TestGroup");
        var parent = permissionGroup.AddPermission("ParentPermission");

        // Act
        var child = parent.AddChild("ChildPermission");

        // Assert
        child.Should().NotBeNull();
        child.Name.Should().Be("ChildPermission");
        parent.Children.Should().Contain(child);
    }

    [Fact]
    public void AddChild_WithDisplayName_SetsDisplayName()
    {
        // Arrange
        var permissionGroup = CreateTestGroup("TestGroup");
        var parent = permissionGroup.AddPermission("Parent");

        // Act
        var child = parent.AddChild("Child", "Child Display");

        // Assert
        child.DisplayName.Should().Be("Child Display");
    }

    [Fact]
    public void PermissionDefinition_IsEnabledDefault_True()
    {
        // Arrange
        var permissionGroup = CreateTestGroup("TestGroup");

        // Act
        var permission = permissionGroup.AddPermission("TestPermission");

        // Assert
        permission.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void PermissionDefinition_IsEnabled_CanBeModified()
    {
        // Arrange
        var permissionGroup = CreateTestGroup("TestGroup");
        var permission = permissionGroup.AddPermission("TestPermission");

        // Act
        permission.IsEnabled = false;

        // Assert
        permission.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void AddChild_MultipleChildren_AllAreAdded()
    {
        // Arrange
        var permissionGroup = CreateTestGroup("TestGroup");
        var parent = permissionGroup.AddPermission("Parent");

        // Act
        var child1 = parent.AddChild("Child1");
        var child2 = parent.AddChild("Child2");
        var child3 = parent.AddChild("Child3");

        // Assert
        parent.Children.Should().HaveCount(3);
        parent.Children.Should().ContainInOrder(child1, child2, child3);
    }

    [Fact]
    public void PermissionDefinition_Children_ReturnsReadOnlyList()
    {
        // Arrange
        var permissionGroup = CreateTestGroup("TestGroup");
        var parent = permissionGroup.AddPermission("Parent");
        parent.AddChild("Child");

        // Act & Assert
        parent.Children.Should().BeAssignableTo<IReadOnlyList<PermissionDefinition>>();
    }

    [Fact]
    public void NestedChildren_MultipleHierarchyLevels()
    {
        // Arrange
        var permissionGroup = CreateTestGroup("TestGroup");
        var parent = permissionGroup.AddPermission("Parent");
        var child = parent.AddChild("Child");
        var grandchild = child.AddChild("Grandchild");

        // Act & Assert
        parent.Children.Should().HaveCount(1).And.Contain(child);
        child.Children.Should().HaveCount(1).And.Contain(grandchild);
        grandchild.Children.Should().BeEmpty();
    }

    [Fact]
    public void PermissionGroup_Name_IsSet()
    {
        // Arrange & Act
        var permissionGroup = CreateTestGroup("TestGroup");

        // Assert
        permissionGroup.Name.Should().Be("TestGroup");
    }

    [Fact]
    public void PermissionGroup_DisplayName_CanBeNull()
    {
        // Arrange & Act
        var permissionGroup = CreateTestGroup("TestGroup");

        // Assert
        permissionGroup.DisplayName.Should().BeNull();
    }

    [Fact]
    public void PermissionGroup_WithDisplayName_SetsDisplayName()
    {
        // Arrange & Act
        var permissionGroup = CreateTestGroup("TestGroup", "Test Group Display");

        // Assert
        permissionGroup.DisplayName.Should().Be("Test Group Display");
    }

    [Fact]
    public void PermissionGroup_Permissions_InitiallyEmpty()
    {
        // Arrange & Act
        var permissionGroup = CreateTestGroup("TestGroup");

        // Assert
        permissionGroup.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void PermissionDefinition_Children_InitiallyEmpty()
    {
        // Arrange
        var permissionGroup = CreateTestGroup("TestGroup");

        // Act
        var permission = permissionGroup.AddPermission("Permission");

        // Assert
        permission.Children.Should().BeEmpty();
    }
}
