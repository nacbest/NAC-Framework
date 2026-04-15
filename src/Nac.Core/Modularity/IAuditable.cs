namespace Nac.Core.Modularity;

/// <summary>
/// Marker interface for commands that should be recorded in the audit trail.
/// The audit behavior logs: who, what, when, which tenant, and data changes.
/// </summary>
public interface IAuditable;
