namespace PropertyManagement.Web.Models.Reviews;

public sealed record QueueRowViewModel
{
    public required int ApplicationId { get; init; }
    public required string PropertyName { get; init; }
    public required string UnitNumber { get; init; }
    public required string ApplicantName { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public string? ClaimedBy { get; init; }
    public DateTime? ClaimedAt { get; init; }
    public bool HasExistingLease { get; init; }
}
