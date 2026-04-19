using FluentAssertions;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Permissions;
using Xunit;

namespace Nac.Identity.Tests.Permissions;

/// <summary>
/// Hierarchical permission definition tests for PermissionDefinitionManager.
/// Tests permission trees and nested permission structures.
/// </summary>
public class PermissionDefinitionManagerHierarchyTests
{
    [Fact]
    public void GetAll_WithHierarchicalPermissions_ReturnsFlattenedList()
    {
        // Arrange
        var provider = new HierarchicalTestPermissionProvider();
        var manager = new PermissionDefinitionManager(new[] { provider });

        // Act
        var allPermissions = manager.GetAll();

        // Assert — should include parent and all children
        allPermissions.Should().HaveCount(3); // Parent + 2 children
        allPermissions.Select(p => p.Name).Should().Contain(new[]
        {
            "Posts.Manage",
            "Posts.Create",
            "Posts.Delete",
        });
    }

    [Fact]
    public void GetOrNull_WithChildPermissionName_ReturnsChildPermission()
    {
        // Arrange
        var provider = new HierarchicalTestPermissionProvider();
        var manager = new PermissionDefinitionManager(new[] { provider });

        // Act
        var childPermission = manager.GetOrNull("Posts.Create");

        // Assert
        childPermission.Should().NotBeNull();
        childPermission!.Name.Should().Be("Posts.Create");
    }

    // ── Test Helpers / Mock Providers ─────────────────────────────────────────

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
}
