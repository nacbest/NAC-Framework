using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Permissions;
using Nac.Identity.Services;
using Xunit;

namespace Nac.Identity.Tests.Permissions;

/// <summary>
/// Batch and composite permission checking tests for PermissionChecker.
/// Tests multiple permission evaluation scenarios.
/// </summary>
public class PermissionCheckerBatchTests
{
    [Fact]
    public async Task IsGrantedAsync_MultiplePermissions_ReturnsCorrectResults()
    {
        // Arrange
        var permissionNames = new[] { "Users.Create", "Users.Read", "Roles.Create" };
        var user = CreateClaimsPrincipal(new[]
        {
            new Claim(NacIdentityClaims.Permission, "Users.Create"),
            new Claim(NacIdentityClaims.Permission, "Users.Read"),
        });
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var manager = CreatePermissionManager();
        var checker = new PermissionChecker(httpContextAccessor, manager);

        // Act
        var result = await checker.IsGrantedAsync(permissionNames);

        // Assert
        result.IsGranted("Users.Create").Should().BeTrue();
        result.IsGranted("Users.Read").Should().BeTrue();
        result.IsGranted("Roles.Create").Should().BeFalse();
        result.AllGranted.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_MultiplePermissions_AllGranted()
    {
        // Arrange
        var permissionNames = new[] { "Users.Create", "Users.Read" };
        var user = CreateClaimsPrincipal(new[]
        {
            new Claim(NacIdentityClaims.Permission, "Users.Create"),
            new Claim(NacIdentityClaims.Permission, "Users.Read"),
        });
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var manager = CreatePermissionManager();
        var checker = new PermissionChecker(httpContextAccessor, manager);

        // Act
        var result = await checker.IsGrantedAsync(permissionNames);

        // Assert
        result.AllGranted.Should().BeTrue();
    }

    [Fact]
    public async Task IsGrantedAsync_WithUserIdParam_ThrowsNotSupported()
    {
        // Arrange — cross-user permission check not yet implemented
        var userId = Guid.NewGuid();
        var httpContextAccessor = CreateHttpContextAccessor(CreateClaimsPrincipal([]));
        var manager = CreatePermissionManager();
        var checker = new PermissionChecker(httpContextAccessor, manager);

        // Act
        var act = () => checker.IsGrantedAsync(userId, "Users.Create");

        // Assert
        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task IsGrantedAsync_MultiplePermissions_WithUnauthenticatedUser_AllDenied()
    {
        // Arrange
        var permissionNames = new[] { "Users.Create", "Users.Read", "Roles.Create" };
        var user = CreateClaimsPrincipal(new Claim[] { }, isAuthenticated: false);
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var manager = CreatePermissionManager();
        var checker = new PermissionChecker(httpContextAccessor, manager);

        // Act
        var result = await checker.IsGrantedAsync(permissionNames);

        // Assert
        result.IsGranted("Users.Create").Should().BeFalse();
        result.IsGranted("Users.Read").Should().BeFalse();
        result.IsGranted("Roles.Create").Should().BeFalse();
        result.AllGranted.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_WithDeepHierarchy_AncestorGrantsDescendant()
    {
        // Arrange — user has "Admin.Access" which is ancestor of "Admin.Users.Create"
        var user = CreateClaimsPrincipal(new[] { new Claim(NacIdentityClaims.Permission, "Admin.Access") });
        var httpContextAccessor = CreateHttpContextAccessor(user);
        var manager = CreatePermissionManagerWithDeepHierarchy();
        var checker = new PermissionChecker(httpContextAccessor, manager);

        // Act
        var result = await checker.IsGrantedAsync("Admin.Users.Create");

        // Assert
        result.Should().BeTrue();
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

    private static PermissionDefinitionManager CreatePermissionManagerWithDeepHierarchy()
    {
        var provider = new DeepHierarchyPermissionProvider();
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

    private sealed class DeepHierarchyPermissionProvider : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext context)
        {
            var group = context.AddGroup("Admin");
            var level1 = group.AddPermission("Admin.Access");
            var level2 = level1.AddChild("Admin.Users");
            level2.AddChild("Admin.Users.Create");
        }
    }
}
