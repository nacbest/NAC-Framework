namespace Nac.Observability.Logging;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nac.Core.Abstractions.Identity;

/// <summary>
/// Middleware that enriches ILogger scope with ICurrentUser context for each request.
/// Place early in the pipeline, after authentication and tenant resolution.
/// </summary>
public sealed class LoggingEnricherMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingEnricherMiddleware> _logger;

    public LoggingEnricherMiddleware(RequestDelegate next, ILogger<LoggingEnricherMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var currentUser = context.RequestServices.GetService<ICurrentUser>();

        string? tenantId = null;
        string? userId = null;

        if (currentUser is { IsAuthenticated: true })
        {
            userId = currentUser.Id.ToString();
            tenantId = currentUser.TenantId;
        }

        using var scope = _logger.BeginNacScope(tenantId: tenantId, userId: userId);
        await _next(context);
    }
}
