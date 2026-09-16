using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PropertyManagement.Core.Dtos;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.ValueObjects;

namespace PropertyManagement.Web.Models.Properties;

public class PropertyFormViewModel
{
    [BindNever]
    public int? Id { get; set; }

    [Required]
    [StringLength(Property.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

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

    public PropertyDetails ToDetails()
    {
        return new PropertyDetails
        {
            Name = Name,
            Street = Street,
            City = City,
            State = State,
            PostalCode = PostalCode
        };
    }
}
