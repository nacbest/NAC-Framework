namespace Nac.Cqrs;

/// <summary>
/// Represents a void return type for commands that produce no result.
/// Use as <c>TResponse</c> when a handler returns nothing meaningful.
/// </summary>
public readonly record struct Unit
{
    /// <summary>The singleton value of <see cref="Unit"/>.</summary>
    public static readonly Unit Value = default;
}
