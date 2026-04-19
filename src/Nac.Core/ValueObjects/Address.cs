using Nac.Core.Domain;
using Nac.Core.Primitives;

namespace Nac.Core.ValueObjects;

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string? State { get; }
    public string Country { get; }
    public string? ZipCode { get; }

    public Address(string street, string city, string country, string? state = null, string? zipCode = null)
    {
        Street = Guard.NotNullOrEmpty(street, nameof(street));
        City = Guard.NotNullOrEmpty(city, nameof(city));
        Country = Guard.NotNullOrEmpty(country, nameof(country));
        State = state;
        ZipCode = zipCode;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return ZipCode;
    }
}
