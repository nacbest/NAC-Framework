namespace Nac.Domain;

/// <summary>
/// Interface for entities that track creation and modification metadata.
/// The persistence layer automatically populates these fields via SaveChanges interceptor.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? LastModifiedAt { get; set; }
    string? LastModifiedBy { get; set; }
}
