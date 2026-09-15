using System.Text.RegularExpressions;
using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.ValueObjects;

namespace PropertyManagement.Core.Validation;

public static partial class ApplicantDetailsRules
{
    public static IReadOnlyList<FieldError> Validate(ApplicantDetails details)
    {
        var errors = new List<FieldError>();

        CheckText(errors, nameof(ApplicantDetails.FirstName), "First name", details.FirstName, Applicant.NameMaxLength);
        CheckText(errors, nameof(ApplicantDetails.LastName), "Last name", details.LastName, Applicant.NameMaxLength);

        var phoneEntered = CheckText(errors, nameof(ApplicantDetails.Phone), "Phone", details.Phone, Applicant.PhoneMaxLength);
        if (phoneEntered && !PhonePattern().IsMatch(details.Phone!.Trim()))
        {
            errors.Add(new FieldError(nameof(ApplicantDetails.Phone), "Phone must be a valid phone number."));
        }

        var emailEntered = CheckText(errors, nameof(ApplicantDetails.Email), "Email", details.Email, Applicant.EmailMaxLength);
        if (emailEntered && !EmailPattern().IsMatch(details.Email!.Trim()))
        {
            errors.Add(new FieldError(nameof(ApplicantDetails.Email), "Email must be a valid email address."));
        }

        CheckText(errors, nameof(ApplicantDetails.Street), "Street", details.Street, Address.StreetMaxLength);
        CheckText(errors, nameof(ApplicantDetails.City), "City", details.City, Address.CityMaxLength);
        CheckText(errors, nameof(ApplicantDetails.State), "State", details.State, Address.StateMaxLength);
        CheckText(errors, nameof(ApplicantDetails.PostalCode), "Postal code", details.PostalCode, Address.PostalCodeMaxLength);

        return errors;
    }

    private static bool CheckText(List<FieldError> errors, string field, string label, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new FieldError(field, $"{label} is required."));
            return false;
        }

        if (value.Trim().Length > maxLength)
        {
            errors.Add(new FieldError(field, $"{label} must be {maxLength} characters or fewer.", BlocksSaving: true));
            return false;
        }

        return true;
    }

    [GeneratedRegex(@"^\+?(?:[\s().-]*\d){7,15}[\s().-]*$")]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
