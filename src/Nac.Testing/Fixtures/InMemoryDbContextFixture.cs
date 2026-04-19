using Microsoft.EntityFrameworkCore;

namespace Nac.Testing.Fixtures;

public class InMemoryDbContextFixture<TContext> : IDisposable
    where TContext : DbContext
{
    private readonly DbContextOptions<TContext> _options;
    private bool _disposed;

    public InMemoryDbContextFixture()
    {
        var databaseName = $"TestDb_{Guid.NewGuid():N}";
        _options = new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    /// <summary>Creates a fresh DbContext instance sharing the same InMemory database.</summary>
    public TContext CreateContext() =>
        (TContext)Activator.CreateInstance(typeof(TContext), _options)!;

    public DbContextOptions<TContext> Options => _options;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
