using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Nac.Identity.Services;
using Xunit;

namespace Nac.Identity.Tests.Services;

public class CurrentUserAccessorTests
{
    private const string TestUserId = "550e8400-e29b-41d4-a716-446655440000";
    private const string TestEmail = "user@example.com";
    private const string TestTenantId = "tenant-123";
    private static readonly Guid TestRoleId1 = Guid.NewGuid();
    private static readonly Guid TestRoleId2 = Guid.NewGuid();

    [Fact]
    public void Id_WithValidNameIdentifierClaim_ReturnsParsedGuid()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, TestUserId) };
        var user = CreateClaimsPrincipal(claims);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act
        var id = accessor.Id;

        // Assert
        id.Should().Be(Guid.Parse(TestUserId));
    }

    [Fact]
    public void Id_WithoutNameIdentifierClaim_ReturnsGuidEmpty()
    {
        // Arrange
        var claims = new Claim[] { };
        var user = CreateClaimsPrincipal(claims);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act
        var id = accessor.Id;

        // Assert
        id.Should().Be(Guid.Empty);
    }


    [Fact]
    public void Email_WithValidEmailClaim_ReturnsEmail()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Email, TestEmail) };
        var user = CreateClaimsPrincipal(claims);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act
        var email = accessor.Email;

        // Assert
        email.Should().Be(TestEmail);
    }

    [Fact]
    public void Email_WithoutEmailClaim_ReturnsNull()
    {
        // Arrange
        var claims = new Claim[] { };
        var user = CreateClaimsPrincipal(claims);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act
        var email = accessor.Email;

        // Assert
        email.Should().BeNull();
    }

    [Fact]
    public void TenantId_WithValidTenantIdClaim_ReturnsTenantId()
    {
        // Arrange
        var claims = new[] { new Claim(NacIdentityClaims.TenantId, TestTenantId) };
        var user = CreateClaimsPrincipal(claims);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act
        var tenantId = accessor.TenantId;

        // Assert
        tenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void RoleIds_WithRoleIdsClaim_ReturnsDeserializedGuids()
    {
        // Arrange — role_ids claim carries a JSON-serialized Guid[]
        var expected = new[] { TestRoleId1, TestRoleId2 };
        var claims = new[]
        {
            new Claim(NacIdentityClaims.RoleIds, JsonSerializer.Serialize(expected)),
        };
        var user = CreateClaimsPrincipal(claims);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act
        var roleIds = accessor.RoleIds;

        // Assert
        roleIds.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void RoleIds_WithoutClaim_ReturnsEmptyList()
    {
        // Arrange
        var user = CreateClaimsPrincipal([]);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act
        var roleIds = accessor.RoleIds;

        // Assert
        roleIds.Should().BeEmpty();
        roleIds.Should().BeAssignableTo<IReadOnlyList<Guid>>();
    }

    [Fact]
    public void IsAuthenticated_WithAuthenticatedPrincipal_ReturnsTrue()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, TestUserId) };
        var user = CreateClaimsPrincipal(claims, isAuthenticated: true);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act
        var isAuthenticated = accessor.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_WithUnauthenticatedPrincipal_ReturnsFalse()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, TestUserId) };
        var user = CreateClaimsPrincipal(claims, isAuthenticated: false);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act
        var isAuthenticated = accessor.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeFalse();
    }


    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ClaimsPrincipal CreateClaimsPrincipal(
        Claim[] claims,
        bool isAuthenticated = true)
    {
        var identity = new ClaimsIdentity(claims, isAuthenticated ? "TestAuth" : null);
        return new ClaimsPrincipal(identity);
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = user;
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        return accessor;
    }
}
