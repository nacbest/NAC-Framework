using Nac.Jobs.Abstractions;

namespace Nac.Testing.Fakes;

/// <summary>
/// In-memory IRecurringJobManager fake for test assertions.
/// </summary>
public sealed class FakeRecurringJobManager : IRecurringJobManager
{
    private readonly Dictionary<string, JobDefinition> _jobs = [];

    public Task AddOrUpdateAsync(string jobId, Type handlerType, string cronExpression,
        CancellationToken ct = default)
    {
        _jobs[jobId] = new JobDefinition
        {
            JobId = jobId,
            HandlerType = handlerType,
            CronExpression = cronExpression
        };
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string jobId, CancellationToken ct = default)
    {
        _jobs.Remove(jobId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<JobDefinition>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<JobDefinition>>(_jobs.Values.ToList());
    }

    /// <summary>
    /// Resets all registered jobs.
    /// </summary>
    public void Reset() => _jobs.Clear();
}
