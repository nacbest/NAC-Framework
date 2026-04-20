namespace Nac.Core.Primitives;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? CreatedBy { get; set; }

    /// <summary>User id (string) of the last writer. During impersonation this is the
    /// tenant-scoped subject (host user id), matching <c>CurrentUser.Id</c>.</summary>
    string? UpdatedBy { get; set; }

    /// <summary>Host user id (string) performing impersonation; <c>null</c> on normal writes.</summary>
    string? ImpersonatorId { get; set; }
}
