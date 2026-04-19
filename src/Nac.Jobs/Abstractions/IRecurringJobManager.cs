namespace Nac.Jobs.Abstractions;

/// <summary>
/// Manages recurring background jobs (CRUD).
/// Consumer provides implementation with cron parsing and persistence.
/// </summary>
public interface IRecurringJobManager
{
    /// <summary>
    /// Creates or updates a recurring job with a cron schedule.
    /// </summary>
    /// <param name="jobId">Unique job identifier.</param>
    /// <param name="handlerType">Type implementing IJobHandler.</param>
    /// <param name="cronExpression">Standard cron expression (5-field).</param>
    Task AddOrUpdateAsync(string jobId, Type handlerType, string cronExpression,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a recurring job by ID.
    /// </summary>
    Task RemoveAsync(string jobId, CancellationToken ct = default);

    /// <summary>
    /// Lists all registered recurring jobs.
    /// </summary>
    Task<IReadOnlyList<JobDefinition>> GetAllAsync(CancellationToken ct = default);
}
