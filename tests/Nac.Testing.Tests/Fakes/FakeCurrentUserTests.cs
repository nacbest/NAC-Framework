using FluentAssertions;
using Nac.Testing.Fakes;
using Xunit;

namespace Nac.Testing.Tests.Fakes;

public class FakeCurrentUserTests
{
    [Fact]
    public void Default_IsAuthenticated()
    {
        var user = new FakeCurrentUser();

        user.IsAuthenticated.Should().BeTrue();
        user.Email.Should().Be("test@example.com");
        user.TenantId.Should().Be("default");
        user.Roles.Should().Contain("User");
        user.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Anonymous_NotAuthenticated()
    {
        var user = FakeCurrentUser.Anonymous();

        user.IsAuthenticated.Should().BeFalse();
        user.Id.Should().Be(Guid.Empty);
        user.Email.Should().BeNull();
        user.Roles.Should().BeEmpty();
    }

    [Fact]
    public void Admin_HasAdminRole()
    {
        var user = FakeCurrentUser.Admin();

        user.Roles.Should().Contain("Admin");
        user.Email.Should().Be("admin@example.com");
        user.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void Create_CustomProperties()
    {
        var id = Guid.NewGuid();
        var user = FakeCurrentUser.Create(id, "custom@example.com", "Admin", "Moderator");

        user.Id.Should().Be(id);
        user.Email.Should().Be("custom@example.com");
        user.Roles.Should().BeEquivalentTo(["Admin", "Moderator"]);
    }
}
