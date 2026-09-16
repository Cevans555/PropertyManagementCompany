namespace PropertyManagement.Web.Queries;

public sealed class LeaseHolding
{
    public int ApplicationId { get; init; }
    public string HolderUserId { get; init; } = string.Empty;
    public int LeaseApplicationId { get; init; }
    public int UnitId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
}
