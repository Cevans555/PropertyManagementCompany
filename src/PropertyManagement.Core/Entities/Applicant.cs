using PropertyManagement.Core.Common;
using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.Validation;

namespace PropertyManagement.Core.Entities;

public class Applicant : AuditableEntity
{
    public const int NameMaxLength = 100;
    public const int PhoneMaxLength = 30;
    public const int EmailMaxLength = 256;
    public int Id { get; private set; }
    public int RentalApplicationId { get; private set; }
    public string UserId { get; private set; }
    public bool IsPrimary { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }

    // Separate nullable fields instead of the Address value object: bonus 4 lets this section be saved
    // with errors, so it can hold a partial address, which Address refuses to create.
    public string? Street { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? PostalCode { get; private set; }
    public DateTime? DetailsSavedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public bool HasSavedDetails => DetailsSavedAt is not null;

    public bool AreDetailsComplete
    {
        get
        {
            if (!HasSavedDetails)
                return false;

            return GetDetailsErrors().Count == 0;
        }
    }

    private Applicant()
    {
        UserId = null!;
    }

    internal Applicant(string userId, bool isPrimary)
    {
        UserId = userId;
        IsPrimary = isPrimary;
    }

    public ApplicantDetails GetDetails()
    {
        return new ApplicantDetails
        {
            FirstName = FirstName,
            LastName = LastName,
            Phone = Phone,
            Email = Email,
            Street = Street,
            City = City,
            State = State,
            PostalCode = PostalCode
        };
    }

    public IReadOnlyList<FieldError> GetDetailsErrors()
    {
        if (!HasSavedDetails)
            return [];

        return ApplicantDetailsRules.Validate(GetDetails());
    }

    internal void SaveDetails(ApplicantDetails details, bool allowInvalid, DateTime now)
    {
        var errors = ApplicantDetailsRules.Validate(details);

        foreach (var error in errors)
        {
            if (error.BlocksSaving || !allowInvalid)
                throw new DomainException(error.Message);
        }

        FirstName = Clean(details.FirstName);
        LastName = Clean(details.LastName);
        Phone = Clean(details.Phone);
        Email = Clean(details.Email);
        Street = Clean(details.Street);
        City = Clean(details.City);
        State = Clean(details.State);
        PostalCode = Clean(details.PostalCode);
        DetailsSavedAt = now;
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }
}
