using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PropertyManagement.Web.Models.Applications;

public class CoApplicantFormViewModel
{
    [BindNever]
    public int ApplicationId { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = "Co-applicant's email")]
    public string Email { get; set; } = string.Empty;
}
