using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.ValueObjects;

namespace PropertyManagement.Tests.TestSupport;

internal static class TestData
{
    public const string ApplicantId = "applicant-1";
    public const string OtherApplicantId = "applicant-2";
    public const string ManagerId = "manager-1";
    public const string OtherManagerId = "manager-2";

    public static readonly DateTime Now = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly Today = DateOnly.FromDateTime(Now);

    public static Address Address()
    {
        return new Address("1 Main St", "Albany", "NY", "12207");
    }

    public static ApplicantDetails Details(
        string? firstName = "Jane",
        string? lastName = "Doe",
        string? phone = "(518) 555-0100",
        string? email = "jane@example.com",
        string? street = "1 Main St",
        string? city = "Albany",
        string? state = "NY",
        string? postalCode = "12207")
    {
        return new ApplicantDetails
        {
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Email = email,
            Street = street,
            City = city,
            State = state,
            PostalCode = postalCode
        };
    }

    public static ResidenceDetails Residence()
    {
        return new ResidenceDetails
        {
            Address = new Address("22 Elm St", "Troy", "NY", "12180"),
            LandlordName = "Bob Landlord",
            LandlordPhone = "(518) 555-0199",
            MoveInDate = new DateOnly(2022, 1, 1),
            MoveOutDate = new DateOnly(2025, 12, 31)
        };
    }

    public static RentalApplication Draft()
    {
        return Draft(Now);
    }

    public static RentalApplication Draft(DateTime now)
    {
        return RentalApplication.Start(1, ApplicantId, unitHasActiveLease: false, now);
    }

    public static RentalApplication ReadyToSubmit(DateTime now)
    {
        var application = Draft(now);
        application.SaveApplicantDetails(ApplicantId, ApplicantId, Details(), now);
        application.AddResidence(ApplicantId, Residence());
        application.SaveResidenceSection(ApplicantId, now);
        return application;
    }

    public static RentalApplication ReadyToSubmit()
    {
        return ReadyToSubmit(Now);
    }

    public static RentalApplication Submitted()
    {
        return Submitted(Now);
    }

    public static RentalApplication Submitted(DateTime now)
    {
        var application = ReadyToSubmit(now);
        application.Submit(ApplicantId, unitHasActiveLease: false, now);
        return application;
    }

    public static RentalApplication Claimed()
    {
        return Claimed(Now);
    }

    public static RentalApplication Claimed(DateTime now)
    {
        var application = Submitted(now);
        application.Claim(ManagerId, now);
        return application;
    }
}
