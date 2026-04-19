using Microsoft.AspNetCore.Mvc;
using Nac.Core.Results;

namespace ReferenceApp.SharedKernel.Results;

/// <summary>
/// Maps <see cref="Result{T}"/> to ASP.NET Core <see cref="IActionResult"/>.
/// Uses the actual <see cref="ResultStatus"/> enum from Nac.Core.
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result) =>
        result.Status switch
        {
            ResultStatus.Ok => new OkObjectResult(result.Value),
            ResultStatus.NotFound => new NotFoundObjectResult(result.Errors.FirstOrDefault()),
            ResultStatus.Forbidden => new ForbidResult(),
            ResultStatus.Invalid => new BadRequestObjectResult(new ValidationProblemDetails(
                result.ValidationErrors
                    .GroupBy(e => e.Identifier)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()))),
            ResultStatus.Conflict => new ConflictObjectResult(result.Errors.FirstOrDefault()),
            ResultStatus.Error => new ObjectResult(result.Errors.FirstOrDefault()) { StatusCode = 500 },
            _ => new ObjectResult(result.Errors.FirstOrDefault()) { StatusCode = 500 }
        };
}
