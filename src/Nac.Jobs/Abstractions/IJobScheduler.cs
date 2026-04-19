namespace Nac.Jobs.Abstractions;

/// <summary>
/// Schedules one-off and delayed background jobs.
/// Consumer provides implementation (e.g., Quartz, Hangfire, BackgroundService).
/// </summary>
public interface IJobScheduler
{
    /// <summary>
    /// Enqueues a job for immediate execution.
    /// </summary>
    /// <returns>Job ID for tracking.</returns>
    Task<string> EnqueueAsync<TJob>(CancellationToken ct = default)
        where TJob : IJobHandler;

    /// <summary>
    /// Schedules a job for execution after a delay.
    /// </summary>
    /// <returns>Job ID for tracking.</returns>
    Task<string> ScheduleAsync<TJob>(TimeSpan delay, CancellationToken ct = default)
        where TJob : IJobHandler;
}
