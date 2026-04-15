namespace Nac.Core;

/// <summary>
/// Base record for strongly-typed identifiers.
/// Prevents mixing up IDs of different entity types at compile time.
/// </summary>
/// <typeparam name="TValue">The underlying value type (typically Guid, long, or string).</typeparam>
/// <example>
/// <code>
/// public sealed record OrderId(Guid Value) : StronglyTypedId&lt;Guid&gt;(Value)
/// {
///     public static OrderId New() => new(Guid.NewGuid());
/// }
/// </code>
/// </example>
public abstract record StronglyTypedId<TValue>(TValue Value)
    where TValue : notnull
{
    public override string ToString() => Value.ToString()!;
}
