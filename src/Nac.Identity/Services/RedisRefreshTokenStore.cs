using System.Text.Json;
using StackExchange.Redis;
using Nac.Identity.Entities;

namespace Nac.Identity.Services;

/// <summary>
/// Redis implementation of refresh token storage.
/// Opt-in for high-scale deployments. Tokens auto-expire via Redis TTL.
/// </summary>
public sealed class RedisRefreshTokenStore : IRefreshTokenStore
{
    private readonly IDatabase _db;

    private const string KeyPrefix = "nac:refresh:";
    private const string UserSetPrefix = "nac:refresh:user:";

    public RedisRefreshTokenStore(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task StoreAsync(RefreshToken token)
    {
        var key = $"{KeyPrefix}{token.TokenHash}";
        var userSetKey = $"{UserSetPrefix}{token.UserId}";
        var ttl = token.ExpiresAt - DateTimeOffset.UtcNow;

        if (ttl <= TimeSpan.Zero)
            return; // Already expired

        var data = new RefreshTokenData
        {
            Id = token.Id,
            UserId = token.UserId,
            TokenHash = token.TokenHash,
            ExpiresAt = token.ExpiresAt,
            CreatedAt = token.CreatedAt,
            DeviceInfo = token.DeviceInfo,
            TenantId = token.TenantId
        };

        var json = JsonSerializer.Serialize(data);

        var batch = _db.CreateBatch();

        // Store token with TTL
        _ = batch.StringSetAsync(key, json, ttl);

        // Add to user's token set (for revoke-all)
        _ = batch.SetAddAsync(userSetKey, token.TokenHash);
        _ = batch.KeyExpireAsync(userSetKey, ttl);

        batch.Execute();
        await Task.CompletedTask;
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
    {
        var key = $"{KeyPrefix}{tokenHash}";
        var json = await _db.StringGetAsync(key);

        if (json.IsNullOrEmpty)
            return null;

        var data = JsonSerializer.Deserialize<RefreshTokenData>(json!);
        if (data is null)
            return null;

        return new RefreshToken
        {
            Id = data.Id,
            UserId = data.UserId,
            TokenHash = data.TokenHash,
            ExpiresAt = data.ExpiresAt,
            CreatedAt = data.CreatedAt,
            DeviceInfo = data.DeviceInfo,
            TenantId = data.TenantId
        };
    }

    public async Task RevokeAsync(string tokenHash)
    {
        var key = $"{KeyPrefix}{tokenHash}";
        await _db.KeyDeleteAsync(key);
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        var userSetKey = $"{UserSetPrefix}{userId}";
        var tokenHashes = await _db.SetMembersAsync(userSetKey);

        if (tokenHashes.Length == 0)
            return;

        var keys = tokenHashes
            .Select(h => (RedisKey)$"{KeyPrefix}{h}")
            .Append(userSetKey)
            .ToArray();

        await _db.KeyDeleteAsync(keys);
    }

    public Task<int> CleanupExpiredAsync()
    {
        // Redis TTL handles expiry automatically
        return Task.FromResult(0);
    }

    /// <summary>
    /// Internal DTO for Redis serialization (excludes navigation properties).
    /// </summary>
    private sealed class RefreshTokenData
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public required string TokenHash { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? DeviceInfo { get; set; }
        public string? TenantId { get; set; }
    }
}
