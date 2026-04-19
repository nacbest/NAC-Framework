using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;
using Xunit;

namespace Nac.Identity.IntegrationTests.Infrastructure;

/// <summary>
/// Shared Postgres container for integration tests. Started once per test run via
/// xUnit's <see cref="IAsyncLifetime"/> on the <see cref="IntegrationCollection"/>.
/// Tests request a per-test database via <see cref="CreateDatabaseAsync"/> so data
/// from one test cannot leak into another.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    /// <summary>
    /// Creates an isolated database (fresh schema via EnsureCreated) and returns its
    /// connection string. Each test class calls this in its constructor so tests do
    /// not interfere with each other.
    /// </summary>
    public async Task<string> CreateDatabaseAsync(string dbName)
    {
        await using var conn = new Npgsql.NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{dbName}\";";
        await cmd.ExecuteNonQueryAsync();

        var builder = new Npgsql.NpgsqlConnectionStringBuilder(ConnectionString) { Database = dbName };
        return builder.ConnectionString;
    }
}

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<PostgresFixture> { }
