using Nac.Core.Primitives;

namespace Nac.Core.ValueObjects;

public sealed class DateRange : ValueObject
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public DateRange(DateTime start, DateTime end)
    {
        if (start > end)
            throw new ArgumentException("Start date must be before or equal to end date.");
        Start = start;
        End = end;
    }

    public bool Contains(DateTime date) => date >= Start && date <= End;

    public bool Overlaps(DateRange other) =>
        Start <= other.End && End >= other.Start;

    public TimeSpan Duration => End - Start;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
