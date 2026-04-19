using Npgsql;

namespace ReferenceApp.IntegrationTests.CrossModule;

/// <summary>
/// End-to-end cross-module tests: Orders → outbox → EventBus → Billing.
///
/// Flow:
///   1. POST /api/orders  → OrdersDbContext saves Order + OutboxEvent in orders schema
///   2. OutboxWorker polls orders.__outbox_events every 5s, publishes OrderCreatedEvent
///   3. InMemoryEventBus dispatches to OrderCreatedEventHandler in Billing
///   4. Handler upserts Customer + creates Invoice in billing schema
///
/// BillingDbContext is internal — assertions use raw Npgsql SQL against billing schema.
/// Column names are PascalCase (EF default, no snake_case convention in migrations).
///
/// OutboxWorker context: NacDbContext alias = OrdersDbContext (last AddNacPersistence wins).
/// AppRootModule DependsOn order: BillingModule first, OrdersModule last.
/// Source: samples/ReferenceApp/src/Host/AppRootModule.cs.
/// </summary>
[Collection("Integration")]
public sealed class OrderCreatedTriggersInvoiceTests(ReferenceAppFactory factory) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await factory.ResetDatabaseAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task PostingOrder_UpsertsCustomerAndCreatesInvoice()
    {
        // Arrange — do NOT pre-seed a Customer; test the upsert (create) path.
        var client = await factory.CreateAuthenticatedClientAsync(
            TestDataSeeder.AdminEmail, TestDataSeeder.TestPassword,
            tenantId: TestDataSeeder.DefaultTenant);

        const decimal unitPrice = 50m;
        const int quantity = 2;
        const decimal expectedTotal = unitPrice * quantity; // 100m

        // Act — create order.
        var createResp = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
        [
            new OrderItemDto(Guid.NewGuid(), Quantity: quantity, UnitPrice: unitPrice),
        ]));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "admin has Orders.Create permission");

        var orderId = await createResp.Content.ReadFromJsonAsync<Guid>();
        orderId.Should().NotBeEmpty();

        // Assert — wait for OutboxWorker to mark OrderCreatedEvent as processed (max 15s;
        // worker polls every 5s so worst case = ~5s + handler execution).
        await factory.OutboxWaitHelper.WaitForOrderCreatedProcessedAsync(
            orderId, timeout: TimeSpan.FromSeconds(15));

        // Assert Invoice and Customer in billing schema via raw SQL.
        // BillingDbContext is internal sealed — cannot resolve from test DI scope.
        await using var conn = new NpgsqlConnection(factory.ConnectionString);
        await conn.OpenAsync();

        var invoice = await QueryInvoiceAsync(conn, orderId);
        invoice.Should().NotBeNull(
            because: $"OrderCreatedEventHandler should have created an invoice for orderId={orderId}");

        invoice!.Amount.Should().Be(expectedTotal,
            because: "invoice amount = sum of order line items (2 × 50)");
        invoice.TenantId.Should().Be(TestDataSeeder.DefaultTenant,
            because: "TenantId propagated via OrderCreatedEvent.TenantId payload");

        var customer = await QueryCustomerAsync(conn, invoice.CustomerId);
        customer.Should().NotBeNull(
            because: "OrderCreatedEventHandler upserts Customer on first order for this user");
        customer!.TenantId.Should().Be(TestDataSeeder.DefaultTenant,
            because: "customer TenantId set from event payload");
    }

    [Fact]
    public async Task PostingOrderTwice_OnlyCreatesInvoiceOnce()
    {
        // Idempotency: simulate outbox redelivery by resetting ProcessedAt after first delivery,
        // then waiting for second processing. Handler must not create a second invoice.
        // Guard mechanism: AnyAsync check in handler + unique index on Invoice.OrderId.

        var client = await factory.CreateAuthenticatedClientAsync(
            TestDataSeeder.AdminEmail, TestDataSeeder.TestPassword,
            tenantId: TestDataSeeder.DefaultTenant);

        // Create order and wait for first successful processing.
        var createResp = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
        [
            new OrderItemDto(Guid.NewGuid(), Quantity: 1, UnitPrice: 75m),
        ]));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = await createResp.Content.ReadFromJsonAsync<Guid>();

        await factory.OutboxWaitHelper.WaitForOrderCreatedProcessedAsync(
            orderId, timeout: TimeSpan.FromSeconds(15));

        // Simulate redelivery: reset ProcessedAt so OutboxWorker picks it up again.
        await using var conn = new NpgsqlConnection(factory.ConnectionString);
        await conn.OpenAsync();
        await ResetOutboxProcessedAtAsync(conn, orderId);

        // Wait for second outbox processing cycle only — do NOT use the invoice-based wait
        // because idempotency means no NEW invoice is created. Poll outbox row directly,
        // then give InMemoryEventBusWorker a small buffer to attempt (and guard) the handler.
        await factory.OutboxWaitHelper.WaitForOutboxRowProcessedAsync(
            orderId, timeout: TimeSpan.FromSeconds(15));

        // Give InMemoryEventBusWorker time to dispatch + handler to complete idempotency guard.
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert — exactly one invoice for this orderId despite two deliveries.
        var invoiceCount = await CountInvoicesByOrderIdAsync(conn, orderId);
        invoiceCount.Should().Be(1,
            because: "OrderCreatedEventHandler is idempotent: AnyAsync guard + unique DB index on OrderId");
    }

    // ── Raw SQL helpers (PascalCase column names — EF default, no snake_case) ─

    private static async Task<InvoiceRow?> QueryInvoiceAsync(NpgsqlConnection conn, Guid orderId)
    {
        const string sql = """
            SELECT "Id", "CustomerId", "OrderId", "Amount", "TenantId"
            FROM billing.invoices
            WHERE "OrderId" = @orderId
            LIMIT 1
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("orderId", orderId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new InvoiceRow(
            Id:         reader.GetGuid(0),
            CustomerId: reader.GetGuid(1),
            OrderId:    reader.GetGuid(2),
            Amount:     reader.GetDecimal(3),
            TenantId:   reader.GetString(4));
    }

    private static async Task<CustomerRow?> QueryCustomerAsync(NpgsqlConnection conn, Guid customerId)
    {
        const string sql = """
            SELECT "Id", "UserId", "Email", "TenantId"
            FROM billing.customers
            WHERE "Id" = @customerId
            LIMIT 1
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("customerId", customerId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new CustomerRow(
            Id:       reader.GetGuid(0),
            UserId:   reader.GetGuid(1),
            Email:    reader.GetString(2),
            TenantId: reader.GetString(3));
    }

    private static async Task ResetOutboxProcessedAtAsync(NpgsqlConnection conn, Guid orderId)
    {
        // Reset the processed outbox row so OutboxWorker picks it up for redelivery.
        // PascalCase column names — must be double-quoted.
        const string sql = """
            UPDATE orders."__outbox_events"
            SET "ProcessedAt" = NULL,
                "RetryCount"  = 0,
                "Error"       = NULL
            WHERE "EventType" LIKE '%OrderCreatedEvent%'
              AND "Payload" LIKE @orderIdPattern
              AND "ProcessedAt" IS NOT NULL
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("orderIdPattern", $"%{orderId}%");
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountInvoicesByOrderIdAsync(NpgsqlConnection conn, Guid orderId)
    {
        const string sql = """
            SELECT COUNT(1) FROM billing.invoices WHERE "OrderId" = @orderId
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("orderId", orderId);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    // ── Private row projections ────────────────────────────────────────────────

    private sealed record InvoiceRow(Guid Id, Guid CustomerId, Guid OrderId, decimal Amount, string TenantId);
    private sealed record CustomerRow(Guid Id, Guid UserId, string Email, string TenantId);
}
