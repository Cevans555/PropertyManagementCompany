using System.ComponentModel.DataAnnotations;
using PropertyManagement.Data;

namespace PropertyManagement.Web.Models.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Choose whether you are an applicant or a property manager.")]
    [Display(Name = "Account type")]
    public string Role { get; set; } = string.Empty;

    [Required]
    [StringLength(AppUser.NameMaxLength)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(AppUser.NameMaxLength)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The {0} must be at least {2} characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
