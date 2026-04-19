using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Permissions;
using Nac.Identity.Services;
using Xunit;

namespace Nac.Identity.Tests.Permissions;

public class PermissionCheckerTests
{
    [Fact]
    public async Task IsGrantedAsync_WithExplicitPermissionClaim_ReturnsTrue()
    {
        // Arrange
        var permissionName = "Users.Create";
        var user = CreateClaimsPrincipal(new[] { new Claim(NacIdentityClaims.Permission, permissionName) });
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var manager = CreatePermissionManager();
        var checker = new PermissionChecker(httpContextAccessor, manager);

        // Act
        var result = await checker.IsGrantedAsync(permissionName);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsGrantedAsync_WithoutPermissionClaim_ReturnsFalse()
    {
        // Arrange
        var permissionName = "Users.Create";
        var user = CreateClaimsPrincipal(new Claim[] { });
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var manager = CreatePermissionManager();
        var checker = new PermissionChecker(httpContextAccessor, manager);

        // Act
        var result = await checker.IsGrantedAsync(permissionName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_WithUnauthenticatedUser_ReturnsFalse()
    {
        // Arrange
        var permissionName = "Users.Create";
        var user = CreateClaimsPrincipal(new Claim[] { }, isAuthenticated: false);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var manager = CreatePermissionManager();
        var checker = new PermissionChecker(httpContextAccessor, manager);

        // Act
        var result = await checker.IsGrantedAsync(permissionName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_WithNullHttpContext_ReturnsFalse()
    {
        // Arrange
        var permissionName = "Users.Create";
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var manager = CreatePermissionManager();
        var checker = new PermissionChecker(httpContextAccessor, manager);

        // Act
        var result = await checker.IsGrantedAsync(permissionName);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_WithUnknownPermissionName_ReturnsFalse()
    {
        // Arrange — user has a permission claim, but requesting a permission not in the manager registry
        var user = CreateClaimsPrincipal(new[] { new Claim(NacIdentityClaims.Permission, "Users.Create") });
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var manager = CreatePermissionManager();
        var checker = new PermissionChecker(httpContextAccessor, manager);

        // Act — requesting a permission that was never defined
        var result = await checker.IsGrantedAsync("Unknown.NotDefined");

        // Assert — permission doesn't exist in registry, so it's denied
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_WithParentPermissionClaim_ChildPermissionGranted()
    {
        // Arrange — user has "Posts.Manage" which is parent of "Posts.Create"
        var user = CreateClaimsPrincipal(new[] { new Claim(NacIdentityClaims.Permission, "Posts.Manage") });
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var manager = CreatePermissionManagerWithHierarchy();
        var checker = new PermissionChecker(httpContextAccessor, manager);

        // Act
        var result = await checker.IsGrantedAsync("Posts.Create");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsGrantedAsync_WithoutParentPermission_ChildPermissionDenied()
    {
        // Arrange — user has no permissions, requesting child permission
        var user = CreateClaimsPrincipal(new Claim[] { });
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var manager = CreatePermissionManagerWithHierarchy();
        var checker = new PermissionChecker(httpContextAccessor, manager);

        // Act
        var result = await checker.IsGrantedAsync("Posts.Create");

        // Assert
        result.Should().BeFalse();
    }



    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ClaimsPrincipal CreateClaimsPrincipal(Claim[] claims, bool isAuthenticated = true)
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

    private static PermissionDefinitionManager CreatePermissionManager()
    {
        var provider = new TestPermissionProvider();
        return new PermissionDefinitionManager(new[] { provider });
    }

    private static PermissionDefinitionManager CreatePermissionManagerWithHierarchy()
    {
        var provider = new HierarchicalPermissionProvider();
        return new PermissionDefinitionManager(new[] { provider });
    }

    private sealed class TestPermissionProvider : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext context)
        {
            var usersGroup = context.AddGroup("Users");
            usersGroup.AddPermission("Users.Create");
            usersGroup.AddPermission("Users.Read");

            var rolesGroup = context.AddGroup("Roles");
            rolesGroup.AddPermission("Roles.Create");
        }
    }

    private sealed class HierarchicalPermissionProvider : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext context)
        {
            var group = context.AddGroup("Posts");
            var parent = group.AddPermission("Posts.Manage");
            parent.AddChild("Posts.Create");
            parent.AddChild("Posts.Delete");
        }
    }
}
