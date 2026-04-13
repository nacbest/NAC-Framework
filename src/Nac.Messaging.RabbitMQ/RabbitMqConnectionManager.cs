using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Nac.Messaging.RabbitMQ;

/// <summary>
/// Manages a singleton RabbitMQ connection with automatic recovery.
/// Both <see cref="RabbitMqEventBus"/> and <see cref="RabbitMqConsumerWorker"/>
/// create their own channels from this shared connection.
/// </summary>
internal sealed class RabbitMqConnectionManager : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqConnectionManager> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnectionManager(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqConnectionManager> logger)
    {
        _logger = logger;

        var opts = options.Value;
        _factory = new ConnectionFactory
        {
            HostName = opts.HostName,
            Port = opts.Port,
            UserName = opts.UserName,
            Password = opts.Password,
            VirtualHost = opts.VirtualHost,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(30),
        };
    }

    /// <summary>
    /// Returns the shared connection, creating it lazily on first call.
    /// Thread-safe via SemaphoreSlim.
    /// </summary>
    public async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            _logger.LogInformation("Creating RabbitMQ connection to {Host}:{Port}",
                _factory.HostName, _factory.Port);

            _connection = await _factory.CreateConnectionAsync(ct);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_connection is { IsOpen: true })
                await _connection.CloseAsync();
            _connection?.Dispose();
        }
        finally
        {
            _lock.Dispose();
        }
    }
}
