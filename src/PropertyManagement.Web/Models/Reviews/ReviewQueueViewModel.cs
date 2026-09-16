namespace PropertyManagement.Web.Models.Reviews;

public sealed record ReviewQueueViewModel
{
    public required IReadOnlyList<QueueRowViewModel> Waiting { get; init; }
    public required IReadOnlyList<QueueRowViewModel> MyClaims { get; init; }
    public required IReadOnlyList<QueueRowViewModel> OtherClaims { get; init; }
}
