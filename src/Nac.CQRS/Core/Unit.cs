namespace Nac.CQRS.Core;

/// <summary>
/// Represents a void return type for the mediator pipeline.
/// Used internally so void commands (ICommand) can share the same behavior pipeline
/// as result-returning commands (ICommand&lt;TResult&gt;).
/// </summary>
public readonly record struct Unit
{
    /// <summary>Singleton value.</summary>
    public static readonly Unit Value = default;

    /// <summary>Pre-completed task returning <see cref="Value"/>.</summary>
    public static readonly Task<Unit> Task = System.Threading.Tasks.Task.FromResult(Value);
}
