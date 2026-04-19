namespace Nac.Jobs.Abstractions;

/// <summary>
/// Contract for a background job handler.
/// Implement this interface to define job execution logic.
/// </summary>
public interface IJobHandler
{
    /// <summary>
    /// Executes the job.
    /// </summary>
    Task ExecuteAsync(CancellationToken ct = default);
}
