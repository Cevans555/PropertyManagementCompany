using PropertyManagement.Core.Common;

namespace PropertyManagement.Core.ValueObjects;

public sealed class Address
{
    public const int StreetMaxLength = 200;
    public const int CityMaxLength = 100;
    public const int StateMaxLength = 50;
    public const int PostalCodeMaxLength = 20;

    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string PostalCode { get; private set; }

    public Address(string street, string city, string state, string postalCode)
    {
        Street = Guard.Required(street, "Street", StreetMaxLength);
        City = Guard.Required(city, "City", CityMaxLength);
        State = Guard.Required(state, "State", StateMaxLength);
        PostalCode = Guard.Required(postalCode, "Postal code", PostalCodeMaxLength);
    }

    public override string ToString()
    {
        return $"{Street}, {City}, {State} {PostalCode}";
    }
}
