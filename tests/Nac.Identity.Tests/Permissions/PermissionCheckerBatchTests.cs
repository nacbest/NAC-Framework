using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Nac.Core.Abstractions.Permissions;
using Nac.Identity.Permissions;
using Nac.Identity.Permissions.Cache;
using Nac.Identity.Permissions.Grants;
using Nac.Identity.Services;
using Xunit;

namespace Nac.Identity.Tests.Permissions;

/// <summary>
/// Batch and composite permission checking tests for PermissionChecker.
/// </summary>
public class PermissionCheckerBatchTests
{
    [Fact]
    public async Task IsGrantedAsync_MultiplePermissions_ReturnsCorrectResults()
    {
        var userId = Guid.NewGuid();
        var (checker, repo) = CreateChecker(userId);
        SetupUserGrants(repo, userId, null, ["Users.Create", "Users.Read"]);

        var result = await checker.IsGrantedAsync(["Users.Create", "Users.Read", "Roles.Create"]);

        result.IsGranted("Users.Create").Should().BeTrue();
        result.IsGranted("Users.Read").Should().BeTrue();
        result.IsGranted("Roles.Create").Should().BeFalse();
        result.AllGranted.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_MultiplePermissions_AllGranted()
    {
        var userId = Guid.NewGuid();
        var (checker, repo) = CreateChecker(userId);
        SetupUserGrants(repo, userId, null, ["Users.Create", "Users.Read"]);

        var result = await checker.IsGrantedAsync(["Users.Create", "Users.Read"]);

        result.AllGranted.Should().BeTrue();
    }

    [Fact]
    public async Task IsGrantedAsync_MultiplePermissions_UnauthenticatedUser_AllDenied()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var httpCtx = new DefaultHttpContext();
        httpCtx.User = new ClaimsPrincipal(new ClaimsIdentity());
        accessor.HttpContext.Returns(httpCtx);
        var checker = BuildChecker(accessor);

        var result = await checker.IsGrantedAsync(["Users.Create", "Users.Read", "Roles.Create"]);

        result.IsGranted("Users.Create").Should().BeFalse();
        result.IsGranted("Users.Read").Should().BeFalse();
        result.IsGranted("Roles.Create").Should().BeFalse();
        result.AllGranted.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_WithDeepHierarchy_AncestorGrantsDescendant()
    {
        var userId = Guid.NewGuid();
        var (checker, repo) = CreateChecker(userId, useDeepHierarchy: true);
        SetupUserGrants(repo, userId, null, ["Admin.Access"]);

        var result = await checker.IsGrantedAsync("Admin.Users.Create");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsGrantedAsync_CrossUser_ReturnsResultForExplicitUserId()
    {
        var requestUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var (checker, repo) = CreateChecker(requestUserId);
        // Target user has the permission
        repo.ListGrantsAsync(PermissionProviderNames.User, targetUserId.ToString(), null,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HashSet<string>(["Users.Create"], StringComparer.Ordinal)));

        var result = await checker.IsGrantedAsync(targetUserId, "Users.Create");

        result.Should().BeTrue();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (PermissionChecker checker, IPermissionGrantRepository repo)
        CreateChecker(Guid userId, bool useDeepHierarchy = false)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "u@example.com"),
        };
        var httpCtx = new DefaultHttpContext();
        httpCtx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpCtx);

        var repo = Substitute.For<IPermissionGrantRepository>();
        var manager = useDeepHierarchy
            ? new PermissionDefinitionManager([new DeepHierarchyPermissionProvider()])
            : new PermissionDefinitionManager([new TestPermissionProvider()]);

        return (BuildChecker(accessor, manager, repo), repo);
    }

    private static PermissionChecker BuildChecker(IHttpContextAccessor accessor,
        PermissionDefinitionManager? manager = null, IPermissionGrantRepository? repo = null)
    {
        repo ??= Substitute.For<IPermissionGrantRepository>();
        manager ??= new PermissionDefinitionManager([new TestPermissionProvider()]);
        return new PermissionChecker(accessor, repo, new PassThroughCache(), manager,
            NullLogger<PermissionChecker>.Instance);
    }

    private static void SetupUserGrants(IPermissionGrantRepository repo, Guid userId,
        string? tenantId, string[] grants)
    {
        repo.ListGrantsAsync(PermissionProviderNames.User, userId.ToString(), tenantId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HashSet<string>(grants, StringComparer.Ordinal)));
    }

    private sealed class PassThroughCache : IPermissionGrantCache
    {
        public Task<HashSet<string>> GetOrLoadAsync(string key,
            Func<CancellationToken, Task<HashSet<string>>> factory,
            TimeSpan ttl, CancellationToken ct = default) => factory(ct);

        public Task InvalidateAsync(string key, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task InvalidateByPatternAsync(string pattern, CancellationToken ct = default) =>
            Task.CompletedTask;
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
