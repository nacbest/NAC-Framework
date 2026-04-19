using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Nac.Identity.Services;
using Xunit;

namespace Nac.Identity.Tests.Services;

/// <summary>
/// Composite tests for CurrentUserAccessor with multiple claims and edge cases.
/// </summary>
public class CurrentUserAccessorCompositeTests
{
    private const string TestUserId = "550e8400-e29b-41d4-a716-446655440000";
    private const string TestEmail = "user@example.com";
    private const string TestTenantId = "tenant-123";
    private const string TestRole1 = "Admin";
    private const string TestRole2 = "User";

    [Fact]
    public void MultipleProperties_WithCompleteUserClaims_AllPropertiesPopulated()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId),
            new Claim(ClaimTypes.Email, TestEmail),
            new Claim(NacIdentityClaims.TenantId, TestTenantId),
            new Claim(ClaimTypes.Role, TestRole1),
            new Claim(ClaimTypes.Role, TestRole2),
        };
        var user = CreateClaimsPrincipal(claims, isAuthenticated: true);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act & Assert
        accessor.Id.Should().Be(Guid.Parse(TestUserId));
        accessor.Email.Should().Be(TestEmail);
        accessor.TenantId.Should().Be(TestTenantId);
        accessor.Roles.Should().HaveCount(2);
        accessor.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void AllProperties_WithMinimalClaims_HandleMissingDataGracefully()
    {
        // Arrange — only NameIdentifier claim present
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, TestUserId) };
        var user = CreateClaimsPrincipal(claims, isAuthenticated: true);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act & Assert — other properties should have defaults
        accessor.Id.Should().Be(Guid.Parse(TestUserId));
        accessor.Email.Should().BeNull();
        accessor.TenantId.Should().Be(string.Empty);
        accessor.Roles.Should().BeEmpty();
        accessor.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void Properties_WithNullHttpContext_AllReturnDefaults()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act & Assert
        accessor.Id.Should().Be(Guid.Empty);
        accessor.Email.Should().BeNull();
        accessor.TenantId.Should().Be(string.Empty);
        accessor.Roles.Should().BeEmpty();
        accessor.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void TenantId_WithoutTenantIdClaim_ReturnsEmptyString()
    {
        // Arrange
        var claims = new Claim[] { };
        var user = CreateClaimsPrincipal(claims);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act
        var tenantId = accessor.TenantId;

        // Assert
        tenantId.Should().Be(string.Empty);
    }

    [Fact]
    public void Roles_WithoutRoleClaims_ReturnsEmptyList()
    {
        // Arrange
        var claims = new Claim[] { };
        var user = CreateClaimsPrincipal(claims);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act
        var roles = accessor.Roles;

        // Assert
        roles.Should().BeEmpty();
    }

    [Fact]
    public void Id_WithInvalidNameIdentifierClaim_ReturnsGuidEmpty()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") };
        var user = CreateClaimsPrincipal(claims);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        // Act
        var id = accessor.Id;

        // Assert
        id.Should().Be(Guid.Empty);
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
