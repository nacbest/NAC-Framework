using Nac.Jobs.Abstractions;

namespace Nac.Testing.Fakes;

/// <summary>
/// In-memory IJobScheduler fake that captures enqueued/scheduled jobs for test assertions.
/// </summary>
public sealed class FakeJobScheduler : IJobScheduler
{
    private int _jobCounter;

    /// <summary>
    /// List of enqueued jobs (Type, Delay, JobId).
    /// </summary>
    public List<(Type HandlerType, TimeSpan? Delay, string JobId)> ScheduledJobs { get; } = [];

    public Task<string> EnqueueAsync<TJob>(CancellationToken ct = default)
        where TJob : IJobHandler
    {
        var jobId = $"fake-job-{++_jobCounter}";
        ScheduledJobs.Add((typeof(TJob), null, jobId));
        return Task.FromResult(jobId);
    }

    public Task<string> ScheduleAsync<TJob>(TimeSpan delay, CancellationToken ct = default)
        where TJob : IJobHandler
    {
        var jobId = $"fake-job-{++_jobCounter}";
        ScheduledJobs.Add((typeof(TJob), delay, jobId));
        return Task.FromResult(jobId);
    }

    /// <summary>
    /// Resets all captured jobs.
    /// </summary>
    public void Reset()
    {
        ScheduledJobs.Clear();
        _jobCounter = 0;
    }
}
