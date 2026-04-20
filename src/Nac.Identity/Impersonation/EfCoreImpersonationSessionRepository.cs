using Microsoft.EntityFrameworkCore;
using Nac.Identity.Context;

namespace Nac.Identity.Impersonation;

/// <summary>
/// EF Core implementation of <see cref="IImpersonationSessionRepository"/>. Writes are
/// deferred to an explicit <see cref="SaveChangesAsync"/> call so callers can batch
/// the session insert with the outbox envelope (phase 07) in a single transaction.
/// </summary>
internal sealed class EfCoreImpersonationSessionRepository(NacIdentityDbContext db)
    : IImpersonationSessionRepository
{
    public Task AddAsync(ImpersonationSession session, CancellationToken ct = default)
    {
        db.Set<ImpersonationSession>().Add(session);
        return Task.CompletedTask;
    }

    public Task<ImpersonationSession?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Set<ImpersonationSession>().FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<ImpersonationSession?> GetByJtiAsync(string jti, CancellationToken ct = default) =>
        db.Set<ImpersonationSession>().FirstOrDefaultAsync(s => s.Jti == jti, ct);

    public Task<int> CountRecentByHostUserAsync(Guid hostUserId, DateTime since, CancellationToken ct = default) =>
        db.Set<ImpersonationSession>()
          .AsNoTracking()
          .CountAsync(s => s.HostUserId == hostUserId && s.IssuedAt >= since, ct);

    public async Task<IReadOnlyList<ImpersonationSession>> ListByTenantAsync(
        string tenantId, int skip, int take, CancellationToken ct = default) =>
        await db.Set<ImpersonationSession>()
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.IssuedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
