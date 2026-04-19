using Nac.Core.Abstractions;

namespace ReferenceApp.Host;

/// <summary>
/// Production implementation of <see cref="IDateTimeProvider"/> that returns
/// the real UTC clock. Required by NAC persistence interceptors
/// (AuditableEntityInterceptor, OutboxInterceptor).
/// Register as singleton in Program.cs before AddNacApplication.
/// </summary>
internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
