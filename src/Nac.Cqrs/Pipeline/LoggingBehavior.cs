using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Nac.Cqrs.Pipeline;

/// <summary>
/// Pipeline behavior that logs request handling with timing information.
/// <para>
/// Logs at <c>Debug</c> level for normal requests and at <c>Warning</c> level
/// when the handler takes longer than 500 ms, flagging slow operations.
/// </para>
/// </summary>
/// <typeparam name="TRequest">The request type passing through this behavior.</typeparam>
/// <typeparam name="TResponse">The response type produced by the pipeline.</typeparam>
internal sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest<TResponse>
{
    private const int SlowRequestThresholdMs = 500;

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes the behavior with a typed logger.
    /// </summary>
    /// <param name="logger">Logger scoped to this behavior's generic type parameters.</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct = default)
    {
        var requestType = typeof(TRequest).Name;

        _logger.LogDebug("Handling {RequestType}", requestType);

        var stopwatch = Stopwatch.StartNew();

        var response = await next().ConfigureAwait(false);

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        if (elapsedMs > SlowRequestThresholdMs)
            _logger.LogWarning("Slow request {RequestType} ({ElapsedMs}ms)", requestType, elapsedMs);
        else
            _logger.LogDebug("Handled {RequestType} in {ElapsedMs}ms", requestType, elapsedMs);

        return response;
    }
}
