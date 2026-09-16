using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using PropertyManagement.Core.Validation;

namespace PropertyManagement.Web.Models.Applications;

public class ApplicationPageViewModel
{
    public ApplicationSection Section { get; set; } = ApplicationSection.Applicant;

    public ApplicantDetailsFormModel ApplicantDetails { get; set; } = new();

    /// <summary>Base64 row version of the current user's applicant row, for stale-save detection.</summary>
    public string? ApplicantRowVersion { get; set; }

    /// <summary>Version of the shared Residence History section, for stale-save detection.</summary>
    public Guid ResidenceSectionVersion { get; set; }

    [BindNever]
    public int Id { get; set; }

    [BindNever, ValidateNever]
    public ApplicationHeaderViewModel Header { get; set; } = null!;

    [BindNever]
    public bool CanEdit { get; set; }

    [BindNever]
    public bool CanWithdraw { get; set; }

    [BindNever]
    public bool CanSeeStatusHistory { get; set; }

    [BindNever]
    public bool CanReview { get; set; }

    [BindNever]
    public bool CanManageNotes { get; set; }

    [BindNever, ValidateNever]
    public string? ClaimedByName { get; set; }

    [BindNever]
    public bool IsClaimedByCurrentUser { get; set; }

    [BindNever]
    public bool IsApplicantOnApplication { get; set; }

    [BindNever, ValidateNever]
    public IReadOnlyList<ApplicantPersonViewModel> People { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<StatusHistoryRowViewModel> StatusHistory { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<SubmitBlocker> SubmitBlockers { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<FieldError> SavedApplicantDetailsErrors { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<string> SavedResidenceErrors { get; set; } = [];
}
