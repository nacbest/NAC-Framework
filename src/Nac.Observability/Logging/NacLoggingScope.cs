namespace Nac.Observability.Logging;

using Microsoft.Extensions.Logging;

/// <summary>
/// ILogger extension for enriching log scopes with NAC context.
/// </summary>
public static class NacLoggingScope
{
    /// <summary>
    /// Begins a log scope enriched with NAC context (TenantId, UserId, CorrelationId).
    /// Null values are omitted from the scope.
    /// </summary>
    public static IDisposable? BeginNacScope(
        this ILogger logger,
        string? tenantId = null,
        string? userId = null,
        string? correlationId = null)
    {
        var state = new Dictionary<string, object?>();

        if (tenantId is not null)
            state["TenantId"] = tenantId;
        if (userId is not null)
            state["UserId"] = userId;
        if (correlationId is not null)
            state["CorrelationId"] = correlationId;

        return state.Count > 0 ? logger.BeginScope(state) : null;
    }
}
