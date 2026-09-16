using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.ValueObjects;

namespace PropertyManagement.Web.Models.Applications.Forms;

public class ResidenceFormViewModel : IValidatableObject
{
    [BindNever]
    public int? Id { get; set; }

    [BindNever]
    public int ApplicationId { get; set; }

    public Guid ResidenceSectionVersion { get; set; }

    [Required]
    [StringLength(Address.StreetMaxLength)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [StringLength(Address.CityMaxLength)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(Address.StateMaxLength)]
    public string State { get; set; } = string.Empty;

    [Required]
    [StringLength(Address.PostalCodeMaxLength)]
    [Display(Name = "Postal code")]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [StringLength(ResidenceHistory.LandlordNameMaxLength)]
    [Display(Name = "Landlord name")]
    public string LandlordName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(ResidenceHistory.LandlordPhoneMaxLength)]
    [Display(Name = "Landlord phone")]
    public string LandlordPhone { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Move-in date")]
    public DateOnly? MoveInDate { get; set; }

    [Required]
    [Display(Name = "Move-out date")]
    public DateOnly? MoveOutDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MoveInDate is { } moveIn && MoveOutDate is { } moveOut && moveOut < moveIn)
            yield return new ValidationResult("Move-out date can't be before the move-in date.", [nameof(MoveOutDate)]);
    }

    public ResidenceInput ToInput()
    {
        return new ResidenceInput
        {
            Street = Street,
            City = City,
            State = State,
            PostalCode = PostalCode,
            LandlordName = LandlordName,
            LandlordPhone = LandlordPhone,
            MoveInDate = MoveInDate!.Value,
            MoveOutDate = MoveOutDate!.Value
        };
    }

    public static ResidenceFormViewModel From(ResidenceHistory residence, int applicationId, Guid sectionVersion)
    {
        return new ResidenceFormViewModel
        {
            Id = residence.Id,
            ApplicationId = applicationId,
            ResidenceSectionVersion = sectionVersion,
            Street = residence.Address.Street,
            City = residence.Address.City,
            State = residence.Address.State,
            PostalCode = residence.Address.PostalCode,
            LandlordName = residence.LandlordName,
            LandlordPhone = residence.LandlordPhone,
            MoveInDate = residence.MoveInDate,
            MoveOutDate = residence.MoveOutDate
        };
    }
}
