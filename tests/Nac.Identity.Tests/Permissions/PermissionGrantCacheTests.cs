using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Nac.Identity.Permissions.Cache;
using Xunit;

namespace Nac.Identity.Tests.Permissions;

public class PermissionGrantCacheTests
{
    private static DistributedPermissionGrantCache CreateCache() =>
        new(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));

    [Fact]
    public async Task GetOrLoadAsync_OnMiss_InvokesFactoryAndCaches()
    {
        var cache = CreateCache();
        var calls = 0;
        Task<HashSet<string>> Factory(CancellationToken _) { calls++; return Task.FromResult(new HashSet<string> { "A", "B" }); }

        var first = await cache.GetOrLoadAsync("k1", Factory, TimeSpan.FromMinutes(1));
        var second = await cache.GetOrLoadAsync("k1", Factory, TimeSpan.FromMinutes(1));

        first.Should().BeEquivalentTo(["A", "B"]);
        second.Should().BeEquivalentTo(["A", "B"]);
        calls.Should().Be(1, "second call must be served from cache");
    }

    [Fact]
    public async Task InvalidateAsync_RemovesKey_NextCallReloads()
    {
        var cache = CreateCache();
        var calls = 0;
        Task<HashSet<string>> Factory(CancellationToken _) { calls++; return Task.FromResult(new HashSet<string> { "X" }); }

        await cache.GetOrLoadAsync("k1", Factory, TimeSpan.FromMinutes(1));
        await cache.InvalidateAsync("k1");
        await cache.GetOrLoadAsync("k1", Factory, TimeSpan.FromMinutes(1));

        calls.Should().Be(2);
    }

    [Fact]
    public async Task InvalidateByPatternAsync_RemovesAllMatchingKeys()
    {
        var cache = CreateCache();
        var roleId = Guid.NewGuid();
        var keyT1 = PermissionCacheKeys.Role(roleId, "tenant-1");
        var keyT2 = PermissionCacheKeys.Role(roleId, "tenant-2");
        var keyOther = PermissionCacheKeys.Role(Guid.NewGuid(), "tenant-1");
        var loadsT1 = 0; var loadsT2 = 0; var loadsOther = 0;

        await cache.GetOrLoadAsync(keyT1, _ => { loadsT1++; return Task.FromResult(new HashSet<string>{"a"}); }, TimeSpan.FromMinutes(1));
        await cache.GetOrLoadAsync(keyT2, _ => { loadsT2++; return Task.FromResult(new HashSet<string>{"b"}); }, TimeSpan.FromMinutes(1));
        await cache.GetOrLoadAsync(keyOther, _ => { loadsOther++; return Task.FromResult(new HashSet<string>{"c"}); }, TimeSpan.FromMinutes(1));

        await cache.InvalidateByPatternAsync(PermissionCacheKeys.RolePattern(roleId));

        await cache.GetOrLoadAsync(keyT1, _ => { loadsT1++; return Task.FromResult(new HashSet<string>{"a"}); }, TimeSpan.FromMinutes(1));
        await cache.GetOrLoadAsync(keyT2, _ => { loadsT2++; return Task.FromResult(new HashSet<string>{"b"}); }, TimeSpan.FromMinutes(1));
        await cache.GetOrLoadAsync(keyOther, _ => { loadsOther++; return Task.FromResult(new HashSet<string>{"c"}); }, TimeSpan.FromMinutes(1));

        loadsT1.Should().Be(2, "tenant-1 key for the role was invalidated");
        loadsT2.Should().Be(2, "tenant-2 key for the role was invalidated");
        loadsOther.Should().Be(1, "different role key must not be affected");
    }

    [Fact]
    public void PermissionCacheKeys_BuildsExpectedShape()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        PermissionCacheKeys.User(userId, "t1")
            .Should().Be($"nac:perm:pn=U:pk={userId}:t=t1");
        PermissionCacheKeys.User(userId, null)
            .Should().Be($"nac:perm:pn=U:pk={userId}:t=_");
        PermissionCacheKeys.RolePattern(userId)
            .Should().Be($"nac:perm:pn=R:pk={userId}:t=*");
    }
}
