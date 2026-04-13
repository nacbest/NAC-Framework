namespace Nac.Messaging.RabbitMQ;

/// <summary>
/// Configuration for the RabbitMQ event bus provider.
/// </summary>
public sealed class RabbitMqOptions
{
    /// <summary>RabbitMQ server hostname.</summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>AMQP port (default 5672).</summary>
    public int Port { get; set; } = 5672;

    /// <summary>Authentication username.</summary>
    public string UserName { get; set; } = "guest";

    /// <summary>Authentication password.</summary>
    public string Password { get; set; } = "guest";

    /// <summary>Virtual host (default "/").</summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>Topic exchange name for integration events.</summary>
    public string ExchangeName { get; set; } = "nac.events";

    /// <summary>
    /// Queue name for this consumer. When empty, a name is auto-generated
    /// from the application name.
    /// </summary>
    public string QueueName { get; set; } = "";

    /// <summary>
    /// Number of unacknowledged messages the broker delivers before waiting for acks.
    /// Higher values increase throughput; lower values improve fairness.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 10;

    /// <summary>Whether exchanges and queues survive broker restarts.</summary>
    public bool Durable { get; set; } = true;
}
