namespace PropertyManagement.Web.Models.Reviews;

public sealed record QueueRowViewModel(
    int ApplicationId,
    string PropertyName,
    string UnitNumber,
    string ApplicantName,
    DateTime? SubmittedAt,
    string? ClaimedBy,
    DateTime? ClaimedAt);
