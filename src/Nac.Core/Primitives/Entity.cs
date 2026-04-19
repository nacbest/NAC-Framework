namespace Nac.Core.Primitives;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected init; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> entity) return false;
        if (ReferenceEquals(this, entity)) return true;
        // Transient entities (default Id) are never equal by value
        if (Id.Equals(default(TId)!) || entity.Id.Equals(default(TId)!)) return false;
        return Id.Equals(entity.Id);
    }

    public bool Equals(Entity<TId>? other) => Equals((object?)other);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) =>
        !Equals(left, right);
}
