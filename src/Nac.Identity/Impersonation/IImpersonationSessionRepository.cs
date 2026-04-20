namespace Nac.Identity.Impersonation;

/// <summary>
/// Persistence port for <see cref="ImpersonationSession"/>. Implementation lives at
/// <see cref="EfCoreImpersonationSessionRepository"/>; EF save calls the auditable
/// interceptor which stamps <c>CreatedBy</c>/<c>UpdatedBy</c> automatically.
/// </summary>
public interface IImpersonationSessionRepository
{
    Task AddAsync(ImpersonationSession session, CancellationToken ct = default);

    Task<ImpersonationSession?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<ImpersonationSession?> GetByJtiAsync(string jti, CancellationToken ct = default);

    /// <summary>Count of sessions minted by <paramref name="hostUserId"/> since <paramref name="since"/> — used by rate limiter.</summary>
    Task<int> CountRecentByHostUserAsync(Guid hostUserId, DateTime since, CancellationToken ct = default);

    /// <summary>Most-recent-first paged listing scoped to a tenant (admin UI).</summary>
    Task<IReadOnlyList<ImpersonationSession>> ListByTenantAsync(
        string tenantId, int skip, int take, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
