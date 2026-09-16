using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Web.Models.Units;

public class UnitFormViewModel
{
    [BindNever]
    public int? Id { get; set; }

    [BindNever]
    public int PropertyId { get; set; }

    [BindNever, ValidateNever]
    public string PropertyName { get; set; } = string.Empty;

    [Required]
    [StringLength(Unit.UnitNumberMaxLength)]
    [Display(Name = "Unit number")]
    public string UnitNumber { get; set; } = string.Empty;

    [Range(0, Unit.MaxBedrooms)]
    public int Bedrooms { get; set; }

    [Range(typeof(decimal), "1", "100000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Monthly rent must be between {1} and {2}.")]
    [Display(Name = "Monthly rent")]
    public decimal MonthlyRent { get; set; }

    [Required(ErrorMessage = "Choose a unit type.")]
    [Display(Name = "Unit type")]
    public int? UnitTypeId { get; set; }

    [BindNever, ValidateNever]
    public IReadOnlyList<SelectListItem> UnitTypeOptions { get; set; } = [];

    public UnitDetails ToDetails()
    {
        return new UnitDetails
        {
            UnitNumber = UnitNumber,
            Bedrooms = Bedrooms,
            MonthlyRent = MonthlyRent,
            UnitTypeId = UnitTypeId ?? 0
        };
    }
}
