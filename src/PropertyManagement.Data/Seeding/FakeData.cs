using Bogus;
using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.ValueObjects;

namespace PropertyManagement.Data.Seeding;

internal static class FakeData
{
    public static readonly string[] ManagerNotes =
    [
        "Called previous landlord; no response yet.",
        "Income looks sufficient for this unit.",
        "Applicant asked about parking availability.",
        "Verify move-in date with the applicant."
    ];

    public static readonly string[] ReturnComments =
    [
        "Please add your residence history for the last three years.",
        "The landlord phone number for your previous residence is not reachable. Please correct it."
    ];

    public static readonly string[] DenyComments =
    [
        "The unit was leased to another applicant.",
        "We could not verify your rental history."
    ];

    public static Address Address(Faker faker)
    {
        return new Address(faker.Address.StreetAddress(), faker.Address.City(), faker.Address.StateAbbr(), faker.Address.ZipCode("#####"));
    }

    public static string Phone(Faker faker)
    {
        return faker.Phone.PhoneNumber("(###) ###-####");
    }

    public static ApplicantDetails ApplicantDetails(Faker faker, AppUser user)
    {
        var address = Address(faker);

        return new ApplicantDetails
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = Phone(faker),
            Email = user.Email,
            Street = address.Street,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode
        };
    }

    public static decimal Rent(Faker faker, int bedrooms)
    {
        return Math.Round(faker.Random.Decimal(900, 1400) + bedrooms * 450, 0);
    }
}
