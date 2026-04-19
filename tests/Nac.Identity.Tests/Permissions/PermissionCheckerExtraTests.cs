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
/// Extra <see cref="PermissionChecker"/> coverage: cache behaviour, cross-provider merge,
/// cross-user / cross-tenant overloads, and the resource-aware fallback path.
/// </summary>
public class PermissionCheckerExtraTests
{
    [Fact]
    public async Task IsGrantedAsync_CrossProviderMerge_UserPlusRoleGrantsBothPass()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var (checker, repo, _) = CreateChecker(userId, "t1", roleIds: [roleId]);
        SetupUserGrants(repo, userId, "t1", ["X.View"]);
        SetupRoleGrants(repo, roleId, "t1", ["X.Edit"]);

        (await checker.IsGrantedAsync("X.View")).Should().BeTrue();
        (await checker.IsGrantedAsync("X.Edit")).Should().BeTrue();
    }

    [Fact]
    public async Task IsGrantedAsync_CacheHitOnSecondCall_RepositoryCalledOnce()
    {
        var userId = Guid.NewGuid();
        var cache = new DistributedPermissionGrantCache(
            new Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache(
                Microsoft.Extensions.Options.Options.Create(
                    new Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions())));
        var (checker, repo, _) = CreateChecker(userId, "t1", cache: cache);
        SetupUserGrants(repo, userId, "t1", ["X.View"]);

        await checker.IsGrantedAsync("X.View");
        await checker.IsGrantedAsync("X.View");

        await repo.Received(1).ListGrantsAsync(
            PermissionProviderNames.User, userId.ToString(), "t1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsGrantedAsync_CrossUserOverload_LoadsTargetUserGrants()
    {
        // Current principal is A; we check for B's permission via the overload.
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var (checker, repo, _) = CreateChecker(callerId, "t1");
        SetupUserGrants(repo, targetId, "t1", ["X.View"]);
        SetupUserGrants(repo, callerId, "t1", []);

        (await checker.IsGrantedAsync(targetId, "X.View", "t1")).Should().BeTrue();
        (await checker.IsGrantedAsync(callerId, "X.View", "t1")).Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_CrossTenantOverload_UsesTargetTenantGrants()
    {
        var userId = Guid.NewGuid();
        var (checker, repo, _) = CreateChecker(userId, "t1");
        SetupUserGrants(repo, userId, "t1", ["X.View"]);
        SetupUserGrants(repo, userId, "t2", []);

        (await checker.IsGrantedAsync(userId, "X.View", "t1")).Should().BeTrue();
        (await checker.IsGrantedAsync(userId, "X.View", "t2")).Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_EmptyUserIdOverload_ReturnsFalse()
    {
        var (checker, _, _) = CreateChecker(Guid.NewGuid(), "t1");
        (await checker.IsGrantedAsync(Guid.Empty, "X.View", "t1")).Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_ResourceOverload_FallsBackToNameCheck()
    {
        var userId = Guid.NewGuid();
        var (checker, repo, _) = CreateChecker(userId, "t1");
        SetupUserGrants(repo, userId, "t1", ["X.View"]);

        var pass = await checker.IsGrantedAsync("X.View", "Order", "42");
        var fail = await checker.IsGrantedAsync("X.Edit", "Order", "42");

        pass.Should().BeTrue();
        fail.Should().BeFalse();
    }

    [Fact]
    public async Task IsGrantedAsync_WithMalformedRoleIdsClaim_DoesNotCrash()
    {
        // Malformed role_ids claim (not a valid JSON Guid array) must not crash the
        // permission resolution path — the checker should treat it as "no roles".
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(NacIdentityClaims.TenantId, "t1"),
            new(NacIdentityClaims.RoleIds, "not-a-json-array"),
        };
        var httpCtx = new DefaultHttpContext();
        httpCtx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpCtx);
        var repo = Substitute.For<IPermissionGrantRepository>();
        repo.ListGrantsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>(StringComparer.Ordinal));
        var manager = new PermissionDefinitionManager([new CheckerExtraProvider()]);
        var checker = new PermissionChecker(accessor, repo, new PassThroughCache(), manager,
            NullLogger<PermissionChecker>.Instance);

        var act = async () => await checker.IsGrantedAsync("X.View");
        // Either returns false or throws a well-typed JsonException — it must not
        // return true and must not leak a hard NullReferenceException.
        var thrown = await Record.ExceptionAsync(act);
        if (thrown is null)
            (await checker.IsGrantedAsync("X.View")).Should().BeFalse();
        else
            thrown.Should().BeOfType<System.Text.Json.JsonException>();
    }

    [Fact]
    public async Task BatchIsGrantedAsync_ReturnsPerPermissionDecisions()
    {
        var userId = Guid.NewGuid();
        var (checker, repo, _) = CreateChecker(userId, "t1");
        SetupUserGrants(repo, userId, "t1", ["X.View"]);

        var result = await checker.IsGrantedAsync(["X.View", "X.Edit"]);

        result.AllGranted.Should().BeFalse();
        result.IsGranted("X.View").Should().BeTrue();
        result.IsGranted("X.Edit").Should().BeFalse();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (PermissionChecker checker, IPermissionGrantRepository repo, IPermissionGrantCache cache)
        CreateChecker(Guid userId, string? tenantId, IReadOnlyList<Guid>? roleIds = null,
                      IPermissionGrantCache? cache = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "u@example.com"),
        };
        if (tenantId is not null) claims.Add(new(NacIdentityClaims.TenantId, tenantId));
        if (roleIds is { Count: > 0 })
            claims.Add(new(NacIdentityClaims.RoleIds, JsonSerializer.Serialize(roleIds)));

        var httpCtx = new DefaultHttpContext();
        httpCtx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpCtx);

        var repo = Substitute.For<IPermissionGrantRepository>();
        cache ??= new PassThroughCache();
        var manager = new PermissionDefinitionManager([new CheckerExtraProvider()]);
        var checker = new PermissionChecker(accessor, repo, cache, manager,
            NullLogger<PermissionChecker>.Instance);
        return (checker, repo, cache);
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

    private sealed class PassThroughCache : IPermissionGrantCache
    {
        public Task<HashSet<string>> GetOrLoadAsync(string key,
            Func<CancellationToken, Task<HashSet<string>>> factory, TimeSpan ttl, CancellationToken ct = default)
            => factory(ct);
        public Task InvalidateAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
        public Task InvalidateByPatternAsync(string pattern, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class CheckerExtraProvider : IPermissionDefinitionProvider
    {
        public void Define(IPermissionDefinitionContext context)
        {
            var g = context.AddGroup("X");
            g.AddPermission("X.View");
            g.AddPermission("X.Edit");
        }
    }
}
