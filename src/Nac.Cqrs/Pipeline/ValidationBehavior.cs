using FluentValidation;
using FluentValidation.Results;
using Nac.Core.Results;

namespace Nac.Cqrs.Pipeline;

/// <summary>
/// Pipeline behavior that runs all registered <see cref="IValidator{TRequest}"/> instances
/// before the handler executes.
/// <para>
/// When validation fails:
/// <list type="bullet">
///   <item>If <typeparamref name="TResponse"/> is a <see cref="Result"/> or <see cref="Result{T}"/>,
///   returns <c>Result.Invalid(errors)</c> — no exception is thrown.</item>
///   <item>Otherwise, throws <see cref="ValidationException"/> with all failures collected.</item>
/// </list>
/// </para>
/// </summary>
/// <typeparam name="TRequest">The request type being validated.</typeparam>
/// <typeparam name="TResponse">The response type produced by the pipeline.</typeparam>
internal sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes the behavior with all validators registered for <typeparamref name="TRequest"/>.
    /// </summary>
    /// <param name="validators">Zero or more validators resolved from DI.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <inheritdoc/>
    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct = default)
    {
        if (!_validators.Any())
            return await next().ConfigureAwait(false);

        // Run all validators concurrently and collect failures.
        var context = new ValidationContext<TRequest>(request);
        var validationTasks = _validators
            .Select(v => v.ValidateAsync(context, ct));

        var results = await Task.WhenAll(validationTasks).ConfigureAwait(false);

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next().ConfigureAwait(false);

        return BuildInvalidResponse(failures);
    }

    /// <summary>
    /// Constructs a failure response from validation failures.
    /// Returns <c>Result.Invalid</c> when <typeparamref name="TResponse"/> is a Result type;
    /// throws <see cref="ValidationException"/> for all other response types.
    /// </summary>
    private static TResponse BuildInvalidResponse(List<ValidationFailure> failures)
    {
        var validationErrors = failures
            .Select(f => new ValidationError(f.PropertyName, f.ErrorMessage))
            .ToArray();

        var responseType = typeof(TResponse);

        // Non-generic Result (command returning plain Result)
        if (responseType == typeof(Result))
        {
            object result = Result.Invalid(validationErrors);
            return (TResponse)result;
        }

        // Generic Result<T> — use the static factory via reflection.
        if (responseType.IsAssignableTo(typeof(Result)))
        {
            var invalidMethod = responseType.GetMethod(
                nameof(Result.Invalid),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                [typeof(ValidationError[])]);

            if (invalidMethod is not null)
            {
                object result = invalidMethod.Invoke(null, [validationErrors])!;
                return (TResponse)result;
            }
        }

        // Response is not a Result type — let the caller handle the exception.
        throw new ValidationException(failures);
    }
}
