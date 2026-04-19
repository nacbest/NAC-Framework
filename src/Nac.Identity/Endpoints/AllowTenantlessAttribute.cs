namespace Nac.Identity.Endpoints;

/// <summary>
/// Marker attribute for Minimal API endpoints that do not require a resolved tenant context.
/// When present on an endpoint's metadata, <see cref="TenantRequiredGateMiddleware"/> skips
/// the tenant-presence check for that request.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class AllowTenantlessAttribute : Attribute { }
