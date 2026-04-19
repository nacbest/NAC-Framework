using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReferenceApp.Host;

/// <summary>
/// Design-time factory for <see cref="AppDbContext"/>.
/// Used exclusively by <c>dotnet ef</c> tooling (migrations, scaffolding).
/// Never resolved at runtime — EF Core discovers it by convention during design-time operations.
/// Uses a placeholder connection string; real connection comes from appsettings at runtime.
/// </summary>
internal sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=referenceapp;Username=admin;Password=123456",
            npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "identity"));

        return new AppDbContext(optionsBuilder.Options);
    }
}
