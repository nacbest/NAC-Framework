namespace Nac.Core;

/// <summary>
/// Abstraction over system clock for testability.
/// Inject this instead of calling DateTimeOffset.UtcNow directly.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// Default implementation that returns the real system time.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
