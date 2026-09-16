using System.ComponentModel.DataAnnotations;
using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.Validation;

namespace PropertyManagement.Web.Models.Applications;

public class ApplicantDetailsFormModel : IValidatableObject
{
    [Display(Name = "First name")]
    public string? FirstName { get; set; }

    [Display(Name = "Last name")]
    public string? LastName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Street { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    [Display(Name = "Postal code")]
    public string? PostalCode { get; set; }

    public ApplicantDetails ToDetails()
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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        return ApplicantDetailsRules.Validate(ToDetails())
            .Select(error => new ValidationResult(error.Message, [error.Field]));
    }
}
