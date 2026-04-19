using Microsoft.AspNetCore.Mvc;
using Nac.Core.Results;

namespace Nac.Identity.Management.Internal;

/// <summary>
/// Maps <see cref="Result"/> / <see cref="Result{T}"/> to <see cref="IActionResult"/>
/// using standard HTTP semantics. Keeps controllers thin and consistent.
/// Mirrors the equivalent helper in <c>Nac.MultiTenancy.Management</c>.
/// </summary>
internal static class ResultActionExtensions
{
    public static IActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.Status switch
        {
            ResultStatus.Ok      => controller.NoContent(),
            ResultStatus.NotFound  => controller.NotFound(MessageOf(result)),
            ResultStatus.Invalid   => controller.UnprocessableEntity(ProblemFor(result, controller)),
            ResultStatus.Conflict  => controller.Conflict(MessageOf(result)),
            ResultStatus.Forbidden => controller.Forbid(),
            _                      => controller.Problem(MessageOf(result), statusCode: 500),
        };

    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        if (result.IsSuccess) return controller.Ok(result.Value);
        return ((Result)result).ToActionResult(controller);
    }

    private static string MessageOf(Result r) =>
        r.Errors.Count > 0 ? string.Join("; ", r.Errors) : "Operation failed.";

    private static ValidationProblemDetails ProblemFor(Result r, ControllerBase controller)
    {
        var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var grp in r.ValidationErrors.GroupBy(e => e.Identifier))
            dict[grp.Key] = grp.Select(e => e.ErrorMessage).ToArray();
        return new ValidationProblemDetails(dict);
    }
}
