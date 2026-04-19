using Microsoft.EntityFrameworkCore;
using ReferenceApp.SharedKernel.Infrastructure;

namespace Billing.Infrastructure;

/// <summary>
/// Applies pending EF Core migrations for <see cref="BillingDbContext"/>.
/// Registered as IMigrationRunner in BillingModule.ConfigureServices.
/// Host resolves all IMigrationRunner registrations and calls RunAsync sequentially at startup.
/// </summary>
internal sealed class BillingMigrationRunner(BillingDbContext dbContext) : IMigrationRunner
{
    public Task RunAsync(CancellationToken cancellationToken = default) =>
        dbContext.Database.MigrateAsync(cancellationToken);
}
