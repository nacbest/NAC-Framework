using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Permissions;
using Xunit;

namespace Nac.Identity.Tests.Permissions;

public class PermissionAuthorizationHandlerTests
{
    private const string TestPermissionName = "Users.Create";

    [Fact]
    public void PermissionAuthorizationHandler_ImplementsAuthorizationHandler()
    {
        // Arrange
        var permissionChecker = Substitute.For<IPermissionChecker>();

        // Act
        var handler = new PermissionAuthorizationHandler(permissionChecker);

        // Assert
        handler.Should().BeAssignableTo<AuthorizationHandler<PermissionRequirement>>();
    }

    [Fact]
    public async Task Handle_WithGrantedPermission_SucceedsContext()
    {
        // Arrange
        var permissionChecker = Substitute.For<IPermissionChecker>();
        permissionChecker.IsGrantedAsync(TestPermissionName).Returns(Task.FromResult(true));

        var context = CreateAuthorizationHandlerContext(new PermissionRequirement(TestPermissionName));
        var handler = new PermissionAuthorizationHandler(permissionChecker);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithDeniedPermission_DoesNotSucceedContext()
    {
        // Arrange
        var permissionChecker = Substitute.For<IPermissionChecker>();
        permissionChecker.IsGrantedAsync(TestPermissionName).Returns(Task.FromResult(false));

        var context = CreateAuthorizationHandlerContext(new PermissionRequirement(TestPermissionName));
        var handler = new PermissionAuthorizationHandler(permissionChecker);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CallsPermissionCheckerWithRequiredPermissionName()
    {
        // Arrange
        var permissionChecker = Substitute.For<IPermissionChecker>();
        permissionChecker.IsGrantedAsync(TestPermissionName).Returns(Task.FromResult(true));

        var requirement = new PermissionRequirement(TestPermissionName);
        var context = CreateAuthorizationHandlerContext(requirement);
        var handler = new PermissionAuthorizationHandler(permissionChecker);

        // Act
        await handler.HandleAsync(context);

        // Assert
        await permissionChecker.Received(1).IsGrantedAsync(TestPermissionName);
    }

    [Fact]
    public async Task Handle_WithDifferentPermissions_CallsCheckerWithCorrectName()
    {
        // Arrange
        var permissionName = "Admin.Access";
        var permissionChecker = Substitute.For<IPermissionChecker>();
        permissionChecker.IsGrantedAsync(permissionName).Returns(Task.FromResult(true));

        var requirement = new PermissionRequirement(permissionName);
        var context = CreateAuthorizationHandlerContext(requirement);
        var handler = new PermissionAuthorizationHandler(permissionChecker);

        // Act
        await handler.HandleAsync(context);

        // Assert
        await permissionChecker.Received(1).IsGrantedAsync(permissionName);
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MultipleRequirements_EachEvaluatedSeparately()
    {
        // Arrange
        var permissionChecker = Substitute.For<IPermissionChecker>();
        permissionChecker.IsGrantedAsync("Users.Create").Returns(Task.FromResult(true));
        permissionChecker.IsGrantedAsync("Users.Delete").Returns(Task.FromResult(false));

        var context1 = CreateAuthorizationHandlerContext(new PermissionRequirement("Users.Create"));
        var context2 = CreateAuthorizationHandlerContext(new PermissionRequirement("Users.Delete"));
        var handler = new PermissionAuthorizationHandler(permissionChecker);

        // Act
        await handler.HandleAsync(context1);
        await handler.HandleAsync(context2);

        // Assert
        context1.HasSucceeded.Should().BeTrue();
        context2.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PermissionCheckerThrows_ExceptionPropagates()
    {
        // Arrange
        var permissionChecker = Substitute.For<IPermissionChecker>();
        permissionChecker.IsGrantedAsync(TestPermissionName)
            .Returns(Task.FromException<bool>(new InvalidOperationException("Checker error")));

        var context = CreateAuthorizationHandlerContext(new PermissionRequirement(TestPermissionName));
        var handler = new PermissionAuthorizationHandler(permissionChecker);

        // Act
        var act = () => handler.HandleAsync(context);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Checker error*");
    }

    [Fact]
    public async Task Handle_WithEmptyPermissionName_StillCallsChecker()
    {
        // Arrange
        var permissionChecker = Substitute.For<IPermissionChecker>();
        permissionChecker.IsGrantedAsync("").Returns(Task.FromResult(false));

        var context = CreateAuthorizationHandlerContext(new PermissionRequirement(""));
        var handler = new PermissionAuthorizationHandler(permissionChecker);

        // Act
        await handler.HandleAsync(context);

        // Assert
        await permissionChecker.Received(1).IsGrantedAsync("");
        context.HasSucceeded.Should().BeFalse();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AuthorizationHandlerContext CreateAuthorizationHandlerContext(PermissionRequirement requirement)
    {
        var user = new System.Security.Claims.ClaimsPrincipal();
        var requirements = new[] { (IAuthorizationRequirement)requirement };
        return new AuthorizationHandlerContext(requirements, user, null);
    }
}
