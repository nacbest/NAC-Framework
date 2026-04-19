namespace Nac.Jobs.Abstractions;

/// <summary>
/// Metadata for a registered job (recurring or one-off).
/// </summary>
public sealed class JobDefinition
{
    /// <summary>
    /// Unique job identifier.
    /// </summary>
    public required string JobId { get; init; }

    /// <summary>
    /// Type implementing IJobHandler for this job.
    /// </summary>
    public required Type HandlerType { get; init; }

    /// <summary>
    /// Cron expression for recurring jobs. Null for one-off jobs.
    /// </summary>
    public string? CronExpression { get; init; }

    /// <summary>
    /// Next scheduled run time. Null if not scheduled.
    /// </summary>
    public DateTimeOffset? NextRunAt { get; init; }

    /// <summary>
    /// Last execution time. Null if never run.
    /// </summary>
    public DateTimeOffset? LastRunAt { get; init; }

    /// <summary>
    /// Whether this is a recurring job (has cron expression).
    /// </summary>
    public bool IsRecurring => CronExpression is not null;
}
