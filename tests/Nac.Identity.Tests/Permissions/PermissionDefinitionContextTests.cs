using FluentAssertions;
using Nac.Identity.Permissions;
using Xunit;

namespace Nac.Identity.Tests.Permissions;

public class PermissionDefinitionContextTests
{
    private const string TestGroupName = "Users";
    private const string TestGroupDisplayName = "User Management";

    [Fact]
    public void AddGroup_WithNewGroup_CreatesAndReturnsGroup()
    {
        // Arrange
        var context = new PermissionDefinitionContext();

        // Act
        var group = context.AddGroup(TestGroupName, TestGroupDisplayName);

        // Assert
        group.Should().NotBeNull();
        group.Name.Should().Be(TestGroupName);
        group.DisplayName.Should().Be(TestGroupDisplayName);
    }

    [Fact]
    public void AddGroup_WithoutDisplayName_CreatesGroupWithoutDisplayName()
    {
        // Arrange
        var context = new PermissionDefinitionContext();

        // Act
        var group = context.AddGroup(TestGroupName);

        // Assert
        group.Should().NotBeNull();
        group.Name.Should().Be(TestGroupName);
        group.DisplayName.Should().BeNull();
    }

    [Fact]
    public void GetGroupOrNull_WithExistingGroup_ReturnsGroup()
    {
        // Arrange
        var context = new PermissionDefinitionContext();
        var addedGroup = context.AddGroup(TestGroupName, TestGroupDisplayName);

        // Act
        var retrievedGroup = context.GetGroupOrNull(TestGroupName);

        // Assert
        retrievedGroup.Should().NotBeNull();
        retrievedGroup!.Name.Should().Be(TestGroupName);
        retrievedGroup.DisplayName.Should().Be(TestGroupDisplayName);
    }

    [Fact]
    public void GetGroupOrNull_WithNonExistentGroup_ReturnsNull()
    {
        // Arrange
        var context = new PermissionDefinitionContext();

        // Act
        var group = context.GetGroupOrNull("NonExistent");

        // Assert
        group.Should().BeNull();
    }

    [Fact]
    public void AddGroup_WithDuplicateGroupName_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new PermissionDefinitionContext();
        context.AddGroup(TestGroupName);

        // Act
        var act = () => context.AddGroup(TestGroupName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*'{TestGroupName}'*");
    }

    [Fact]
    public void AddGroup_MultipleGroupsWithDifferentNames_AllRetrievable()
    {
        // Arrange
        var context = new PermissionDefinitionContext();
        var group1 = context.AddGroup("Group1");
        var group2 = context.AddGroup("Group2");

        // Act
        var retrieved1 = context.GetGroupOrNull("Group1");
        var retrieved2 = context.GetGroupOrNull("Group2");

        // Assert
        retrieved1.Should().NotBeNull();
        retrieved2.Should().NotBeNull();
        retrieved1!.Name.Should().Be("Group1");
        retrieved2!.Name.Should().Be("Group2");
    }

    [Fact]
    public void Groups_ReturnsAllAddedGroups()
    {
        // Arrange
        var context = new PermissionDefinitionContext();
        context.AddGroup("Group1");
        context.AddGroup("Group2");
        context.AddGroup("Group3");

        // Act
        var groups = context.Groups;

        // Assert
        groups.Should().HaveCount(3);
        groups.Keys.Should().Contain(new[] { "Group1", "Group2", "Group3" });
    }

    [Fact]
    public void Groups_WithNoAddedGroups_ReturnsEmptyDictionary()
    {
        // Arrange
        var context = new PermissionDefinitionContext();

        // Act
        var groups = context.Groups;

        // Assert
        groups.Should().BeEmpty();
    }

    [Fact]
    public void AddGroup_GroupNameIsCaseSensitive()
    {
        // Arrange
        var context = new PermissionDefinitionContext();
        context.AddGroup("TestGroup");

        // Act
        var retrieved1 = context.GetGroupOrNull("TestGroup");
        var retrieved2 = context.GetGroupOrNull("testgroup");
        var retrieved3 = context.GetGroupOrNull("TESTGROUP");

        // Assert
        retrieved1.Should().NotBeNull();
        retrieved2.Should().BeNull();
        retrieved3.Should().BeNull();
    }

    [Fact]
    public void AddedGroup_CanAddPermissions()
    {
        // Arrange
        var context = new PermissionDefinitionContext();
        var group = context.AddGroup(TestGroupName);

        // Act
        var permission = group.AddPermission("Users.Create", "Create User");

        // Assert
        permission.Should().NotBeNull();
        permission.Name.Should().Be("Users.Create");
        group.Permissions.Should().HaveCount(1);
    }
}
