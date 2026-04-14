using Microsoft.EntityFrameworkCore;
using Nac.Identity.Data;
using Nac.Identity.Entities;

namespace Nac.Identity.Services;

/// <summary>
/// EF Core implementation of refresh token storage.
/// Default store; works with any EF-compatible database.
/// </summary>
public sealed class EfRefreshTokenStore : IRefreshTokenStore
{
    private readonly NacIdentityDbContext _dbContext;

    public EfRefreshTokenStore(NacIdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task StoreAsync(RefreshToken token)
    {
        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
    {
        return await _dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                t.RevokedAt == null &&
                t.ExpiresAt > DateTimeOffset.UtcNow);
    }

    public async Task RevokeAsync(string tokenHash)
    {
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAt == null);

        if (token is not null)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<int> CleanupExpiredAsync()
    {
        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);

        var expiredTokens = await _dbContext.RefreshTokens
            .Where(t =>
                (t.RevokedAt != null && t.RevokedAt < thirtyDaysAgo) ||
                (t.ExpiresAt < thirtyDaysAgo))
            .ToListAsync();

        _dbContext.RefreshTokens.RemoveRange(expiredTokens);
        await _dbContext.SaveChangesAsync();

        return expiredTokens.Count;
    }
}
