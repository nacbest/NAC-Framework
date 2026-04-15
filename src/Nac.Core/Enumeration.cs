using System.Collections.Concurrent;
using System.Reflection;

namespace Nac.Core;

/// <summary>
/// Type-safe enumeration base class. Provides richer behavior than C# enums
/// while remaining persistence-friendly (stores int Id in database).
/// </summary>
/// <example>
/// <code>
/// public sealed class OrderStatus : Enumeration
/// {
///     public static readonly OrderStatus Pending = new(1, nameof(Pending));
///     public static readonly OrderStatus Confirmed = new(2, nameof(Confirmed));
///     public static readonly OrderStatus Shipped = new(3, nameof(Shipped));
///
///     private OrderStatus(int id, string name) : base(id, name) { }
/// }
/// </code>
/// </example>
public abstract class Enumeration : IComparable<Enumeration>, IEquatable<Enumeration>
{
    public int Id { get; }
    public string Name { get; }

    protected Enumeration(int id, string name)
    {
        Id = id;
        Name = name;
    }

    private static readonly ConcurrentDictionary<Type, IReadOnlyList<Enumeration>> Cache = new();

    /// <summary>Returns all declared instances of the enumeration type. Results are cached.</summary>
    public static IReadOnlyList<TEnum> GetAll<TEnum>() where TEnum : Enumeration
        => (IReadOnlyList<TEnum>)Cache.GetOrAdd(typeof(TEnum), static t =>
            (IReadOnlyList<Enumeration>)t
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(f => f.FieldType == t)
                .Select(f => (Enumeration)f.GetValue(null)!)
                .ToList());

    /// <summary>Returns the enumeration instance with the specified ID, or null.</summary>
    public static TEnum? FromId<TEnum>(int id) where TEnum : Enumeration
        => GetAll<TEnum>().FirstOrDefault(e => e.Id == id);

    /// <summary>Returns the enumeration instance with the specified name (case-insensitive), or null.</summary>
    public static TEnum? FromName<TEnum>(string name) where TEnum : Enumeration
        => GetAll<TEnum>().FirstOrDefault(e =>
            string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

    public int CompareTo(Enumeration? other) => other is null ? 1 : Id.CompareTo(other.Id);

    public bool Equals(Enumeration? other)
        => other is not null && GetType() == other.GetType() && Id == other.Id;

    public override bool Equals(object? obj)
        => obj is Enumeration other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public override string ToString() => Name;

    public static bool operator ==(Enumeration? left, Enumeration? right)
        => Equals(left, right);

    public static bool operator !=(Enumeration? left, Enumeration? right)
        => !Equals(left, right);
}
