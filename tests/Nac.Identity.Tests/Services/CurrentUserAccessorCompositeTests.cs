using System.Security.Claims;
using System.Text.Json;
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

    [Fact]
    public void MultipleProperties_WithCompleteClaims_AllPopulated()
    {
        var roleIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId),
            new Claim(ClaimTypes.Email, TestEmail),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(NacIdentityClaims.TenantId, TestTenantId),
            new Claim(NacIdentityClaims.RoleIds, JsonSerializer.Serialize(roleIds)),
        };
        var accessor = CreateAccessor(claims, isAuthenticated: true);

        accessor.Id.Should().Be(Guid.Parse(TestUserId));
        accessor.Email.Should().Be(TestEmail);
        accessor.Name.Should().Be("Test User");
        accessor.TenantId.Should().Be(TestTenantId);
        accessor.RoleIds.Should().BeEquivalentTo(roleIds);
        accessor.IsAuthenticated.Should().BeTrue();
        accessor.IsHost.Should().BeFalse();
    }

    [Fact]
    public void AllProperties_WithMinimalClaims_HandleMissingDataGracefully()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, TestUserId) };
        var accessor = CreateAccessor(claims, isAuthenticated: true);

        accessor.Id.Should().Be(Guid.Parse(TestUserId));
        accessor.Email.Should().BeNull();
        accessor.Name.Should().BeNull();
        accessor.TenantId.Should().BeNull();
        accessor.RoleIds.Should().BeEmpty();
        accessor.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void Properties_WithNullHttpContext_AllReturnDefaults()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var accessor = new CurrentUserAccessor(httpContextAccessor);

        accessor.Id.Should().Be(Guid.Empty);
        accessor.Email.Should().BeNull();
        accessor.Name.Should().BeNull();
        accessor.TenantId.Should().BeNull();
        accessor.RoleIds.Should().BeEmpty();
        accessor.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void TenantId_WithoutClaim_ReturnsNull()
    {
        var accessor = CreateAccessor([], isAuthenticated: true);

        accessor.TenantId.Should().BeNull();
    }

    [Fact]
    public void RoleIds_WithoutClaim_ReturnsEmptyList()
    {
        var accessor = CreateAccessor([], isAuthenticated: true);

        accessor.RoleIds.Should().BeEmpty();
    }

    [Fact]
    public void Id_WithInvalidNameIdentifierClaim_ReturnsGuidEmpty()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") };
        var accessor = CreateAccessor(claims);

        accessor.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public void IsHost_WithIsHostClaim_ReturnsTrue()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId),
            new Claim(NacIdentityClaims.IsHost, "true"),
        };
        var accessor = CreateAccessor(claims, isAuthenticated: true);

        accessor.IsHost.Should().BeTrue();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CurrentUserAccessor CreateAccessor(Claim[] claims, bool isAuthenticated = true)
    {
        var identity = new ClaimsIdentity(claims, isAuthenticated ? "TestAuth" : null);
        var httpCtx = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpCtx);
        return new CurrentUserAccessor(accessor);
    }
}
