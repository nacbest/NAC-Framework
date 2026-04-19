using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Nac.Identity.Permissions;
using Xunit;

namespace Nac.Identity.Tests.Permissions;

public class PermissionRequirementTests
{
    private const string TestPermissionName = "Users.Create";

    [Fact]
    public void Constructor_WithPermissionName_SetsPermissionNameCorrectly()
    {
        // Act
        var requirement = new PermissionRequirement(TestPermissionName);

        // Assert
        requirement.PermissionName.Should().Be(TestPermissionName);
    }

    [Fact]
    public void PermissionRequirement_ImplementsIAuthorizationRequirement()
    {
        // Act
        var requirement = new PermissionRequirement(TestPermissionName);

        // Assert
        requirement.Should().BeAssignableTo<IAuthorizationRequirement>();
    }

    [Fact]
    public void PermissionName_IsReadOnly()
    {
        // Arrange
        var requirement = new PermissionRequirement(TestPermissionName);

        // Act & Assert — PermissionName has only getter, no setter
        // Verify the property is truly read-only by checking reflection
        var property = typeof(PermissionRequirement).GetProperty(nameof(PermissionRequirement.PermissionName));
        property!.CanWrite.Should().BeFalse();
        property.CanRead.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithEmptyPermissionName_SetsEmptyString()
    {
        // Act
        var requirement = new PermissionRequirement("");

        // Assert
        requirement.PermissionName.Should().Be("");
    }

    [Fact]
    public void Constructor_WithComplexPermissionName_PreservesName()
    {
        // Arrange
        var permissionName = "Admin.Users.Management.Create.BulkOperation";

        // Act
        var requirement = new PermissionRequirement(permissionName);

        // Assert
        requirement.PermissionName.Should().Be(permissionName);
    }

    [Fact]
    public void TwoRequirements_WithSamePermissionName_AreNotEqual()
    {
        // Arrange
        var requirement1 = new PermissionRequirement(TestPermissionName);
        var requirement2 = new PermissionRequirement(TestPermissionName);

        // Act & Assert — they are different objects, so not equal by reference
        requirement1.Should().NotBeSameAs(requirement2);
    }

    [Fact]
    public void PermissionRequirement_CanBeUsedInAuthorizationPolicies()
    {
        // This test documents that PermissionRequirement implements the interface
        // correctly and can be passed to authorization handler methods
        var requirement = new PermissionRequirement("Admin.Access");

        // Assert that it's a valid IAuthorizationRequirement
        ((object)requirement).Should().BeAssignableTo<IAuthorizationRequirement>();
    }
}
