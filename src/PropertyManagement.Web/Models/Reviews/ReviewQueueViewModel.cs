namespace PropertyManagement.Web.Models.Reviews;

public sealed record ReviewQueueViewModel(
    IReadOnlyList<QueueRowViewModel> Waiting,
    IReadOnlyList<QueueRowViewModel> MyClaims,
    IReadOnlyList<QueueRowViewModel> OtherClaims);
