namespace Nac.MultiTenancy.Factory;

/// <summary>
/// Resolves the database connection string for a given tenant identifier.
/// Implementations may look up per-tenant connection strings from a store,
/// fall back to a shared connection string, or apply any other routing strategy.
/// </summary>
public interface ITenantConnectionStringResolver
{
    /// <summary>
    /// Returns the connection string to use for the specified tenant.
    /// </summary>
    /// <param name="tenantId">The unique tenant identifier.</param>
    /// <returns>A non-null, non-empty connection string.</returns>
    string Resolve(string tenantId);
}
