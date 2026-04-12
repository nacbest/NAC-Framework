using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nac.Mediator.Abstractions;
using Nac.Mediator.Core;

namespace Nac.Observability;

/// <summary>
/// Command pipeline behavior that logs entry, exit, duration, and errors.
/// Typically registered as the outermost behavior so it captures the full pipeline duration.
/// </summary>
public sealed class LoggingCommandBehavior<TCommand, TResponse>
    : ICommandBehavior<TCommand, TResponse>
{
    private readonly ILogger<LoggingCommandBehavior<TCommand, TResponse>> _logger;

    public LoggingCommandBehavior(ILogger<LoggingCommandBehavior<TCommand, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> HandleAsync(
        TCommand command,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var commandName = typeof(TCommand).Name;

        _logger.LogInformation("Handling command {CommandName}", commandName);

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await next(ct);
            sw.Stop();

            _logger.LogInformation(
                "Handled command {CommandName} in {ElapsedMs}ms",
                commandName, sw.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Command {CommandName} failed after {ElapsedMs}ms",
                commandName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Query pipeline behavior that logs entry, exit, duration, and errors.
/// </summary>
public sealed class LoggingQueryBehavior<TQuery, TResponse>
    : IQueryBehavior<TQuery, TResponse>
{
    private readonly ILogger<LoggingQueryBehavior<TQuery, TResponse>> _logger;

    public LoggingQueryBehavior(ILogger<LoggingQueryBehavior<TQuery, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> HandleAsync(
        TQuery query,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var queryName = typeof(TQuery).Name;

        _logger.LogInformation("Handling query {QueryName}", queryName);

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await next(ct);
            sw.Stop();

            _logger.LogInformation(
                "Handled query {QueryName} in {ElapsedMs}ms",
                queryName, sw.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Query {QueryName} failed after {ElapsedMs}ms",
                queryName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
