using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nac.Core.Results;

namespace Nac.WebApi.ExceptionHandling;

/// <summary>
/// Maps <see cref="Result"/> and <see cref="Result{T}"/> to appropriate <see cref="IActionResult"/>
/// based on <see cref="ResultStatus"/>.
/// </summary>
public static class ResultToHttpMapper
{
    /// <summary>
    /// Converts a non-generic <see cref="Result"/> to an <see cref="IActionResult"/>.
    /// Success returns 204 No Content; failures map to the corresponding HTTP error status.
    /// </summary>
    public static IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();

        return ToProblemResult(result);
    }

    /// <summary>
    /// Converts a generic <see cref="Result{T}"/> to an <see cref="IActionResult"/>.
    /// Success returns 200 OK with the value; failures map to the corresponding HTTP error status.
    /// </summary>
    public static IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return ToProblemResult(result);
    }

    private static IActionResult ToProblemResult(Result result)
    {
        return result.Status switch
        {
            ResultStatus.Invalid => CreateValidationProblem(result),
            ResultStatus.NotFound => CreateProblem(StatusCodes.Status404NotFound, "Not Found", result),
            ResultStatus.Forbidden => CreateProblem(StatusCodes.Status403Forbidden, "Forbidden", result),
            ResultStatus.Conflict => CreateProblem(StatusCodes.Status409Conflict, "Conflict", result),
            ResultStatus.Error => CreateProblem(
                StatusCodes.Status500InternalServerError, "Internal Server Error", result),
            _ => CreateProblem(
                StatusCodes.Status500InternalServerError, "Internal Server Error", result),
        };
    }

    private static IActionResult CreateValidationProblem(Result result)
    {
        var errors = result.ValidationErrors
            .GroupBy(e => e.Identifier)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return new ObjectResult(new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
        })
        {
            StatusCode = StatusCodes.Status400BadRequest,
        };
    }

    private static IActionResult CreateProblem(int statusCode, string title, Result result)
    {
        var detail = result.Errors.Count > 0
            ? string.Join("; ", result.Errors)
            : null;

        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
        })
        {
            StatusCode = statusCode,
        };
    }
}
