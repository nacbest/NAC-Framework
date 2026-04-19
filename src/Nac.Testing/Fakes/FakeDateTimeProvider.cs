using Nac.Core.Abstractions;

namespace Nac.Testing.Fakes;

public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
}
