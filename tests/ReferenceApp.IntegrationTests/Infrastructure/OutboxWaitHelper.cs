using Npgsql;

namespace ReferenceApp.IntegrationTests.Infrastructure;

/// <summary>
/// Polls orders."__outbox_events" and billing.invoices via raw SQL to detect
/// full end-to-end event delivery: Order → outbox → EventBus → Invoice created.
///
/// Race condition mitigation:
///   OutboxWorker marks ProcessedAt BEFORE InMemoryEventBusWorker finishes dispatching
///   (it writes to the in-memory channel then marks processed synchronously).
///   Polling ProcessedAt alone is insufficient — we must also wait for the downstream
///   effect (Invoice row) to appear, using a separate billing.invoices poll.
///
/// Column names: PascalCase (EF default, no snake_case convention configured).
/// Schema: orders (not public). See OrdersDbContext.OnModelCreating.
/// </summary>
public sealed class OutboxWaitHelper(string connectionString)
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Waits until the full cross-module flow completes:
    ///   1. orders.__outbox_events row for orderId has ProcessedAt IS NOT NULL
    ///   2. billing.invoices row for orderId exists (handler has committed)
    ///
    /// Polling billing.invoices directly avoids the race condition where OutboxWorker
    /// sets ProcessedAt before InMemoryEventBusWorker finishes dispatching to the handler.
    /// </summary>
    public async Task WaitForOrderCreatedProcessedAsync(Guid orderId, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var ct = cts.Token;

        while (!ct.IsCancellationRequested)
        {
            if (await IsInvoiceCreatedAsync(orderId, ct))
                return;

            try { await Task.Delay(PollInterval, ct); }
            catch (OperationCanceledException) { break; }
        }

        // Final check — avoids false timeout when invoice appeared just as cts fired.
        if (await IsInvoiceCreatedAsync(orderId, CancellationToken.None))
            return;

        // Provide diagnostic info in timeout message.
        var outboxProcessed = await IsOutboxProcessedAsync(orderId, CancellationToken.None);
        throw new TimeoutException(
            $"Invoice for orderId={orderId} was not created within {timeout.TotalSeconds}s. " +
            $"OutboxWorker processed={outboxProcessed}. " +
            "Check: (1) OutboxWorker running in test host, " +
            "(2) InMemoryEventBusWorker dispatching, " +
            "(3) OrderCreatedEventHandler not throwing silently.");
    }

    /// <summary>
    /// Polls outbox only (used for idempotency reset scenario where we wait for
    /// second processing cycle — InMemoryEventBusWorker race is acceptable there
    /// since we then count invoices directly).
    /// </summary>
    public async Task WaitForOutboxRowProcessedAsync(Guid orderId, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var ct = cts.Token;

        while (!ct.IsCancellationRequested)
        {
            if (await IsOutboxProcessedAsync(orderId, ct))
                return;

            try { await Task.Delay(PollInterval, ct); }
            catch (OperationCanceledException) { break; }
        }

        if (await IsOutboxProcessedAsync(orderId, CancellationToken.None))
            return;

        throw new TimeoutException(
            $"OrderCreatedEvent outbox row for orderId={orderId} not processed within {timeout.TotalSeconds}s.");
    }

    // ── Private SQL polling ───────────────────────────────────────────────────

    private async Task<bool> IsInvoiceCreatedAsync(Guid orderId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        const string sql = """
            SELECT COUNT(1) FROM billing.invoices WHERE "OrderId" = @orderId
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("orderId", orderId);

        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result) > 0;
    }

    private async Task<bool> IsOutboxProcessedAsync(Guid orderId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        // PascalCase columns: "EventType", "Payload", "ProcessedAt".
        const string sql = """
            SELECT COUNT(1)
            FROM orders."__outbox_events"
            WHERE "EventType" LIKE '%OrderCreatedEvent%'
              AND "Payload" LIKE @orderIdPattern
              AND "ProcessedAt" IS NOT NULL
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("orderIdPattern", $"%{orderId}%");

        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result) > 0;
    }
}
