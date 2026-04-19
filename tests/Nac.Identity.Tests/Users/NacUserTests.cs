using FluentAssertions;
using Nac.Core.Primitives;
using Nac.Identity.Users;
using Xunit;

namespace Nac.Identity.Tests.Users;

public class NacUserTests
{
    private const string TestEmail = "test@example.com";

    [Fact]
    public void Constructor_WithEmail_SetsPropertiesCorrectly()
    {
        var user = new NacUser(TestEmail);

        user.Email.Should().Be(TestEmail);
        user.UserName.Should().Be(TestEmail);
        user.Id.Should().NotBe(Guid.Empty);
        user.FullName.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmailAndFullName_SetsFullName()
    {
        var user = new NacUser(TestEmail, "John Doe");

        user.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void Constructor_GeneratesUniqueId()
    {
        var user1 = new NacUser(TestEmail);
        var user2 = new NacUser(TestEmail);

        user1.Id.Should().NotBe(user2.Id);
    }

    [Fact]
    public void NacUser_ImplementsIAuditableEntity()
    {
        var user = new NacUser(TestEmail);

        user.Should().BeAssignableTo<IAuditableEntity>();
        user.CreatedAt.Should().Be(default);
        user.UpdatedAt.Should().BeNull();
        user.CreatedBy.Should().BeNull();
    }

    [Fact]
    public void NacUser_ImplementsISoftDeletable()
    {
        var user = new NacUser(TestEmail);

        user.Should().BeAssignableTo<ISoftDeletable>();
        user.IsDeleted.Should().BeFalse();
        user.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void IsActive_DefaultsToTrue()
    {
        var user = new NacUser(TestEmail);

        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsHost_DefaultsToFalse()
    {
        var user = new NacUser(TestEmail);

        user.IsHost.Should().BeFalse();
    }

    [Fact]
    public void FullName_CanBeSet()
    {
        var user = new NacUser(TestEmail);
        user.FullName = "Jane Smith";

        user.FullName.Should().Be("Jane Smith");
    }

    [Fact]
    public void AuditableProperties_CanBeSet()
    {
        var user = new NacUser(TestEmail);
        var now = DateTime.UtcNow;

        user.CreatedAt = now;
        user.CreatedBy = "admin";
        user.UpdatedAt = now.AddHours(1);

        user.CreatedAt.Should().Be(now);
        user.CreatedBy.Should().Be("admin");
        user.UpdatedAt.Should().Be(now.AddHours(1));
    }

    [Fact]
    public void SoftDeleteProperties_CanBeSet()
    {
        var user = new NacUser(TestEmail);
        var now = DateTime.UtcNow;

        user.IsDeleted = true;
        user.DeletedAt = now;

        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().Be(now);
    }

    [Fact]
    public void IsActive_CanBeDisabled()
    {
        var user = new NacUser(TestEmail);
        user.IsActive = false;

        user.IsActive.Should().BeFalse();
    }
}
