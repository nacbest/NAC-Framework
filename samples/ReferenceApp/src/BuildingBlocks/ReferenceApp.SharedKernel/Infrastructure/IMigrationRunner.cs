namespace ReferenceApp.SharedKernel.Infrastructure;

/// <summary>
/// Contract for a module-owned migration runner.
/// Each module implements this to apply its own EF Core migrations at host startup.
/// Registered as a scoped service in each module's ConfigureServices.
/// Host resolves IEnumerable&lt;IMigrationRunner&gt; and calls RunAsync in registration order.
/// </summary>
public interface IMigrationRunner
{
    /// <summary>Applies pending EF Core migrations for this module's DbContext.</summary>
    Task RunAsync(CancellationToken cancellationToken = default);
}
