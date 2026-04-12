namespace Nac.Modularity;

/// <summary>
/// Marker interface for commands that require an explicit database transaction.
/// The UnitOfWork pipeline behavior wraps the handler in a transaction when this marker is present.
/// Note: All commands go through UoW by default. This marker is for when you need
/// to explicitly signal transactional intent (e.g., for documentation or conditional behavior).
/// </summary>
public interface ITransactional;
