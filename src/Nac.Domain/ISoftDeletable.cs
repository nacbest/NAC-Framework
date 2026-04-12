namespace Nac.Domain;

/// <summary>
/// Interface for entities that support soft deletion.
/// The persistence layer applies a global query filter to exclude soft-deleted entities
/// and sets these fields automatically on <c>Remove</c> operations.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
