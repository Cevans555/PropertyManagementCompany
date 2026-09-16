using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Web.Models.Reviews;

public class ReviewFormViewModel : IValidatableObject
{
    public const string InvalidOutcomeMessage = "Choose a valid review outcome.";

    [BindNever]
    public int ApplicationId { get; set; }

    [BindNever, ValidateNever]
    public string UnitLabel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose an outcome.")]
    public ReviewOutcome? Outcome { get; set; }

    [StringLength(StatusHistory.CommentMaxLength)]
    public string? Comment { get; set; }

    [Display(Name = "Lease start date")]
    public DateOnly? LeaseStartDate { get; set; }

    [BindNever]
    public DateOnly Today { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Outcome is not null && !Enum.IsDefined(Outcome.Value))
        {
            yield return new ValidationResult(InvalidOutcomeMessage, [nameof(Outcome)]);
            yield break;
        }

        if (Outcome is ReviewOutcome.Return or ReviewOutcome.Deny && string.IsNullOrWhiteSpace(Comment))
            yield return new ValidationResult("A comment is required to return or deny an application.", [nameof(Comment)]);

        if (Outcome is ReviewOutcome.Approve && LeaseStartDate is null)
            yield return new ValidationResult("Choose the lease start date.", [nameof(LeaseStartDate)]);
    }
}
