namespace Nac.Core.Abstractions;

/// <summary>
/// Represents a unit of work for coordinating transactional persistence.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
