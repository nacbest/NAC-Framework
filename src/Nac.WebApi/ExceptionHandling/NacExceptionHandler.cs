using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Nac.WebApi.ExceptionHandling;

/// <summary>
/// Global exception handler that converts unhandled exceptions into RFC 9457 ProblemDetails responses.
/// Registered via <c>services.AddExceptionHandler&lt;NacExceptionHandler&gt;()</c>.
/// </summary>
internal sealed class NacExceptionHandler(ILogger<NacExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, errors) = MapException(exception);

        if (statusCode >= 500)
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            logger.LogWarning(exception, "Request error ({StatusCode}): {Message}", statusCode, exception.Message);

        httpContext.Response.StatusCode = statusCode;

        if (errors is not null)
        {
            var validationProblem = new ValidationProblemDetails(errors)
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path,
            };
            validationProblem.Extensions["traceId"] = GetTraceId(httpContext);
            await httpContext.Response.WriteAsJsonAsync(validationProblem, cancellationToken);
        }
        else
        {
            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path,
            };
            problem.Extensions["traceId"] = GetTraceId(httpContext);
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        }

        return true;
    }

    private static (int StatusCode, string Title, string? Detail, IDictionary<string, string[]>? Errors)
        MapException(Exception exception) => exception switch
    {
        ValidationException validationEx => (
            StatusCodes.Status400BadRequest,
            "Validation Failed",
            "One or more validation errors occurred.",
            validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
        ),
        // Infrastructure exceptions: use generic messages. Framework-thrown
        // messages (e.g. ArgumentNullException parameter names, internal lookup keys)
        // can leak implementation details. Domain code should use the Result pattern
        // or throw FluentValidation.ValidationException for user-facing messages.
        UnauthorizedAccessException => (
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "You are not authorized to perform this action.",
            null
        ),
        KeyNotFoundException => (
            StatusCodes.Status404NotFound,
            "Not Found",
            "The requested resource was not found.",
            null
        ),
        ArgumentException => (
            StatusCodes.Status400BadRequest,
            "Bad Request",
            "The request was invalid.",
            null
        ),
        _ => (
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            "An unexpected error occurred.",
            null
        )
    };

    private static string GetTraceId(HttpContext httpContext) =>
        Activity.Current?.Id ?? httpContext.TraceIdentifier;
}
