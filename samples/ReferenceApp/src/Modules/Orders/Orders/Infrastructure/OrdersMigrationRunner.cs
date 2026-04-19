using Microsoft.EntityFrameworkCore;
using ReferenceApp.SharedKernel.Infrastructure;

namespace Orders.Infrastructure;

/// <summary>
/// Applies pending EF Core migrations for <see cref="OrdersDbContext"/>.
/// Registered as IMigrationRunner in OrdersModule.ConfigureServices.
/// Host resolves all IMigrationRunner registrations and calls RunAsync sequentially at startup.
/// </summary>
internal sealed class OrdersMigrationRunner(OrdersDbContext dbContext) : IMigrationRunner
{
    public Task RunAsync(CancellationToken cancellationToken = default) =>
        dbContext.Database.MigrateAsync(cancellationToken);
}
