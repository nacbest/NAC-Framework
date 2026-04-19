using FluentAssertions;
using Nac.Core.Domain;
using Nac.Core.Primitives;
using Nac.Identity.Users;
using Xunit;

namespace Nac.Identity.Tests.Users;

public class NacUserTests
{
    private const string TestEmail = "test@example.com";
    private const string TestTenantId = "tenant-123";

    [Fact]
    public void Constructor_WithEmailAndTenantId_SetsPropertiesCorrectly()
    {
        // Act
        var user = new NacUser(TestEmail, TestTenantId);

        // Assert
        user.Email.Should().Be(TestEmail);
        user.UserName.Should().Be(TestEmail);
        user.TenantId.Should().Be(TestTenantId);
        user.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Constructor_WithEmailAndTenantId_GeneratesUniqueId()
    {
        // Act
        var user1 = new NacUser(TestEmail, TestTenantId);
        var user2 = new NacUser(TestEmail, TestTenantId);

        // Assert
        user1.Id.Should().NotBe(user2.Id);
    }

    [Fact]
    public void NacUser_ImplementsITenantEntity()
    {
        // Act
        var user = new NacUser(TestEmail, TestTenantId);

        // Assert
        user.Should().BeAssignableTo<ITenantEntity>();
        user.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void NacUser_ImplementsIAuditableEntity()
    {
        // Act
        var user = new NacUser(TestEmail, TestTenantId);

        // Assert
        user.Should().BeAssignableTo<IAuditableEntity>();
        // Properties should be initialized (defaults)
        user.CreatedAt.Should().Be(default);
        user.UpdatedAt.Should().BeNull();
        user.CreatedBy.Should().BeNull();
    }

    [Fact]
    public void NacUser_ImplementsISoftDeletable()
    {
        // Act
        var user = new NacUser(TestEmail, TestTenantId);

        // Assert
        user.Should().BeAssignableTo<ISoftDeletable>();
        user.IsDeleted.Should().BeFalse();
        user.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void IsActive_DefaultsToTrue()
    {
        // Act
        var user = new NacUser(TestEmail, TestTenantId);

        // Assert
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void FullName_CanBeSet()
    {
        // Arrange
        var user = new NacUser(TestEmail, TestTenantId);
        const string fullName = "John Doe";

        // Act
        user.FullName = fullName;

        // Assert
        user.FullName.Should().Be(fullName);
    }

    [Fact]
    public void AuditableProperties_CanBeSet()
    {
        // Arrange
        var user = new NacUser(TestEmail, TestTenantId);
        var now = DateTime.UtcNow;
        const string createdBy = "admin";

        // Act
        user.CreatedAt = now;
        user.CreatedBy = createdBy;
        user.UpdatedAt = now.AddHours(1);

        // Assert
        user.CreatedAt.Should().Be(now);
        user.CreatedBy.Should().Be(createdBy);
        user.UpdatedAt.Should().Be(now.AddHours(1));
    }

    [Fact]
    public void SoftDeleteProperties_CanBeSet()
    {
        // Arrange
        var user = new NacUser(TestEmail, TestTenantId);
        var now = DateTime.UtcNow;

        // Act
        user.IsDeleted = true;
        user.DeletedAt = now;

        // Assert
        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().Be(now);
    }

    [Fact]
    public void IsActive_CanBeDisabled()
    {
        // Arrange
        var user = new NacUser(TestEmail, TestTenantId);

        // Act
        user.IsActive = false;

        // Assert
        user.IsActive.Should().BeFalse();
    }
}
