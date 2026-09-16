namespace PropertyManagement.Web.Models.Home;

public sealed class HomePageViewModel
{
    public required bool IsAuthenticated { get; init; }

    public required bool IsManager { get; init; }

    public string? DisplayName { get; init; }

    public int WaitingToReview { get; init; }

    public int ClaimedByMe { get; init; }

    public int PropertyCount { get; init; }

    public int AvailableUnitCount { get; init; }

    public int NeedsAttention { get; init; }

    public int AwaitingDecision { get; init; }

    public int Approved { get; init; }
}
