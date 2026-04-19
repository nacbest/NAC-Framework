using FluentAssertions;
using Nac.Identity.Users;
using Xunit;

namespace Nac.Identity.Tests.Users;

public class NacRoleTests
{
    private const string TestRoleName = "Administrator";
    private const string TestDescription = "Full system access";

    [Fact]
    public void Constructor_WithRoleName_SetsPropertiesCorrectly()
    {
        // Act
        var role = new NacRole(TestRoleName);

        // Assert
        role.Name.Should().Be(TestRoleName);
        role.Id.Should().NotBe(Guid.Empty);
        role.Description.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithRoleNameAndDescription_SetsPropertiesCorrectly()
    {
        // Act
        var role = new NacRole(TestRoleName, description: TestDescription);

        // Assert
        role.Name.Should().Be(TestRoleName);
        role.Description.Should().Be(TestDescription);
        role.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Constructor_GeneratesUniqueId()
    {
        // Act
        var role1 = new NacRole(TestRoleName);
        var role2 = new NacRole(TestRoleName);

        // Assert
        role1.Id.Should().NotBe(role2.Id);
    }

    [Fact]
    public void Description_CanBeSet()
    {
        // Arrange
        var role = new NacRole(TestRoleName);

        // Act
        role.Description = TestDescription;

        // Assert
        role.Description.Should().Be(TestDescription);
    }

    [Fact]
    public void ProtectedConstructor_RequiredByEfCore()
    {
        // Arrange & Act - This just verifies the protected parameterless constructor exists
        // We can verify this by reflection or by calling internal instantiation
        var ctor = typeof(NacRole).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            [],
            null);

        // Assert
        ctor.Should().NotBeNull();
    }
}
