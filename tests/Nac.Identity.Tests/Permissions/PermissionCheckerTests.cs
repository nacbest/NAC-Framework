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

public class PermissionCheckerTests
{
    [Fact]
    public async Task IsGrantedAsync_WithExplicitUserGrant_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var (checker, repo, _) = CreateChecker(userId, tenantId: null);
        SetupUserGrants(repo, userId, null, ["Users.Create"]);

        var result = await checker.IsGrantedAsync("Users.Create");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsGrantedAsync_WithoutGrant_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var (checker, repo, _) = CreateChecker(userId, tenantId: null);
        SetupUserGrants(repo, userId, null, []);

        var result = await checker.IsGrantedAsync("Users.Create");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_WithUnauthenticatedUser_ReturnsFalse()
    {
        var httpCtx = new DefaultHttpContext();
        httpCtx.User = new ClaimsPrincipal(new ClaimsIdentity());
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpCtx);
        var checker = BuildChecker(accessor, CreateManager());

        var result = await checker.IsGrantedAsync("Users.Create");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_WithNullHttpContext_ReturnsFalse()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var checker = BuildChecker(accessor, CreateManager());

        var result = await checker.IsGrantedAsync("Users.Create");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_WithUnknownPermissionName_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var (checker, repo, _) = CreateChecker(userId, tenantId: null);
        SetupUserGrants(repo, userId, null, ["Users.Create"]);

        var result = await checker.IsGrantedAsync("Unknown.NotDefined");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_WithParentGrant_ChildPermissionGranted()
    {
        var userId = Guid.NewGuid();
        var (checker, repo, _) = CreateChecker(userId, tenantId: null, useHierarchy: true);
        SetupUserGrants(repo, userId, null, ["Posts.Manage"]);

        var result = await checker.IsGrantedAsync("Posts.Create");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsGrantedAsync_WithoutParentGrant_ChildPermissionDenied()
    {
        var userId = Guid.NewGuid();
        var (checker, repo, _) = CreateChecker(userId, tenantId: null, useHierarchy: true);
        SetupUserGrants(repo, userId, null, []);

        var result = await checker.IsGrantedAsync("Posts.Create");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_WithRoleGrant_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var (checker, repo, _) = CreateChecker(userId, tenantId: "t1", roleIds: [roleId]);
        SetupUserGrants(repo, userId, "t1", []);
        SetupRoleGrants(repo, roleId, "t1", ["Roles.Create"]);

        var result = await checker.IsGrantedAsync("Roles.Create");

        result.Should().BeTrue();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (PermissionChecker checker, IPermissionGrantRepository repo, IPermissionGrantCache cache)
        CreateChecker(Guid userId, string? tenantId, IReadOnlyList<Guid>? roleIds = null,
            bool useHierarchy = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "u@example.com"),
        };
        if (tenantId is not null)
            claims.Add(new(NacIdentityClaims.TenantId, tenantId));
        if (roleIds?.Count > 0)
            claims.Add(new(NacIdentityClaims.RoleIds, JsonSerializer.Serialize(roleIds)));

        var httpCtx = new DefaultHttpContext();
        httpCtx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpCtx);

        var repo = Substitute.For<IPermissionGrantRepository>();
        var cache = new PassThroughCache();
        var manager = useHierarchy ? CreateHierarchyManager() : CreateManager();

        return (BuildChecker(accessor, manager, repo, cache), repo, cache);
    }

    private static PermissionChecker BuildChecker(IHttpContextAccessor accessor,
        PermissionDefinitionManager manager, IPermissionGrantRepository? repo = null,
        IPermissionGrantCache? cache = null)
    {
        repo ??= Substitute.For<IPermissionGrantRepository>();
        cache ??= new PassThroughCache();
        return new PermissionChecker(accessor, repo, cache, manager,
            NullLogger<PermissionChecker>.Instance);
    }

    private static void SetupUserGrants(IPermissionGrantRepository repo, Guid userId,
        string? tenantId, string[] grants)
    {
        repo.ListGrantsAsync(PermissionProviderNames.User, userId.ToString(), tenantId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HashSet<string>(grants, StringComparer.Ordinal)));
    }

    private static void SetupRoleGrants(IPermissionGrantRepository repo, Guid roleId,
        string? tenantId, string[] grants)
    {
        repo.ListGrantsAsync(PermissionProviderNames.Role, roleId.ToString(), tenantId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new HashSet<string>(grants, StringComparer.Ordinal)));
    }

    private static PermissionDefinitionManager CreateManager()
    {
        return new PermissionDefinitionManager([new TestPermissionProvider()]);
    }

    private static PermissionDefinitionManager CreateHierarchyManager()
    {
        return new PermissionDefinitionManager([new HierarchicalPermissionProvider()]);
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
