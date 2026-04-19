using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Billing.Infrastructure;

/// <summary>
/// Design-time factory used exclusively by <c>dotnet ef</c> tooling (migrations, scaffolding).
/// Never resolved at runtime — EF Core discovers it by convention during design-time operations.
/// Uses a placeholder connection string; real connection string comes from appsettings at runtime.
/// </summary>
internal sealed class BillingDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    public BillingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BillingDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=referenceapp;Username=postgres;Password=postgres",
            npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "billing"));

        return new BillingDbContext(optionsBuilder.Options);
    }
}
