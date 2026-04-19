namespace Nac.Core.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
