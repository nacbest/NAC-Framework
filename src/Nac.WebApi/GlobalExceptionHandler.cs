using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nac.Core.Exceptions;

namespace Nac.WebApi;

/// <summary>
/// Catches all exceptions thrown during request processing and converts them
/// to a standard <see cref="ErrorResponse"/> envelope. <see cref="NacException"/>
/// subclasses map to their declared HTTP status codes; unhandled exceptions
/// return 500 with a correlation ID (no stack trace leaked).
/// </summary>
internal sealed class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NacValidationException ex)
        {
            var details = ex.Errors
                .SelectMany(e => e.Value.Select(msg => new ApiErrorDetail(e.Key, msg)))
                .ToList();

            await WriteErrorAsync(context, ex.StatusCode, "VALIDATION_FAILED", ex.Message, details);
        }
        catch (NacException ex)
        {
            var code = ex switch
            {
                NacUnauthorizedException => "UNAUTHORIZED",
                NacForbiddenException => "FORBIDDEN",
                NacNotFoundException => "NOT_FOUND",
                NacConflictException => "CONFLICT",
                NacDomainException => "DOMAIN_ERROR",
                _ => "ERROR",
            };

            await WriteErrorAsync(context, ex.StatusCode, code, ex.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected — no response needed
        }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            _logger.LogError(ex, "Unhandled exception. TraceId: {TraceId}", traceId);

            await WriteErrorAsync(context, 500, "INTERNAL_ERROR",
                $"An unexpected error occurred. Reference: {traceId}");
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        IReadOnlyList<ApiErrorDetail>? details = null)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Error = new ApiError(code, message, details),
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
