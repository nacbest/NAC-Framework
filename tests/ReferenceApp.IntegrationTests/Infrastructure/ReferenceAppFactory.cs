using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nac.MultiTenancy.Abstractions;
using Nac.MultiTenancy.Context;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace ReferenceApp.IntegrationTests.Infrastructure;

/// <summary>
/// Shared WebApplicationFactory for all integration tests.
/// Lifecycle: one PostgreSqlContainer spun up per test run (IAsyncLifetime),
/// shared across all tests in the [Collection("Integration")] collection.
///
/// Per-test reset: call ResetDatabaseAsync() in each test's constructor or
/// a shared fixture to Respawn all tables, then re-seed test users.
///
/// Connection string override: injected via ConfigureAppConfiguration BEFORE
/// AddNacPersistence evaluates, so all three DbContexts (AppDbContext,
/// OrdersDbContext, BillingDbContext) receive the container URL.
///
/// Tenant header: X-Tenant-Id (from NacTenantHeaders.TenantId constant,
/// verified in src/Nac.MultiTenancy/Resolution/NacTenantHeaders.cs).
/// </summary>
public sealed class ReferenceAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("referenceapp_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    private Respawner? _respawner;

    // Expose for tests that need raw connection (OutboxWaitHelper).
    public string ConnectionString => _postgres.GetConnectionString();

    public OutboxWaitHelper OutboxWaitHelper { get; private set; } = null!;

    // ── IAsyncLifetime ────────────────────────────────────────────────────────

    // xunit.v3 IAsyncLifetime uses ValueTask (not Task).
    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        OutboxWaitHelper = new OutboxWaitHelper(ConnectionString);

        // Trigger WebApplicationFactory build (runs migrations + seed via Program startup).
        // CreateClient() materialises the host.
        _ = CreateClient();

        // Initialise Respawner after schema is created by migrations.
        // Tables to reset: all except EF migrations history tables in all schemas.
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter        = DbAdapter.Postgres,
            SchemasToInclude = ["public", "identity", "orders", "billing"],
            TablesToIgnore   =
            [
                new Respawn.Graph.Table("__EFMigrationsHistory", "identity"),
                new Respawn.Graph.Table("__EFMigrationsHistory", "orders"),
                new Respawn.Graph.Table("__EFMigrationsHistory", "billing"),
            ],
        });
    }

    public new async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    // ── WebApplicationFactory override ────────────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override connection string so all DbContexts use the container.
        // ConfigureAppConfiguration runs before ConfigureServices so AddNacPersistence
        // reads the overridden value when it calls configuration.GetConnectionString("Default").
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
            });
        });

        // Set environment to Development so Program.cs runs migrations + seed on startup.
        builder.UseEnvironment("Development");

        // Replace the singleton ITenantStore with one that includes both "default" and
        // "tenant-b" so cross-tenant isolation tests can send X-Tenant-Id: tenant-b
        // and have HeaderTenantStrategy resolve it to a real tenant (not fall back to default).
        builder.ConfigureTestServices(services =>
        {
            // Replace ITenantStore with one that includes "default" + "tenant-b".
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITenantStore));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddSingleton<ITenantStore>(new InMemoryTenantStore(
            [
                new TenantInfo { Id = "default",  Name = "Default Tenant",  IsActive = true },
                new TenantInfo { Id = "tenant-b", Name = "Tenant B",        IsActive = true },
            ]));
        });

    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Resets all data via Respawn then re-seeds test users.
    /// Call at the start of each test to ensure isolation.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        if (_respawner is not null)
            await _respawner.ResetAsync(conn);

        // Re-seed test users after wipe.
        await using var scope = Services.CreateAsyncScope();
        await TestDataSeeder.SeedAsync(scope.ServiceProvider);
    }

    /// <summary>
    /// Creates an HttpClient pre-configured with a Bearer JWT for the given credentials.
    /// Also sets X-Tenant-Id header so HeaderTenantStrategy resolves the tenant.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email,
        string password,
        string tenantId = TestDataSeeder.DefaultTenant)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);

        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password,
        });

        if (!loginResp.IsSuccessStatusCode)
        {
            var body = await loginResp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Login failed for {email} (HTTP {(int)loginResp.StatusCode}): {body}");
        }

        var payload = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.Token);

        return client;
    }

    // ── Private DTOs ──────────────────────────────────────────────────────────

    private sealed record LoginResponse(string Token, Guid UserId, string Email, string TenantId);
}

/// <summary>
/// xUnit collection definition — single shared factory for all integration tests.
/// Disables parallel execution (xunit.runner.json: parallelizeTestCollections=false).
/// </summary>
[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<ReferenceAppFactory> { }
