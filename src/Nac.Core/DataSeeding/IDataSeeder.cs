namespace Nac.Core.DataSeeding;

public interface IDataSeeder
{
    Task SeedAsync(DataSeedContext context);
}
