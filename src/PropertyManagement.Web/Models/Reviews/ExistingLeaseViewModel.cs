namespace PropertyManagement.Web.Models.Reviews;

public sealed record ExistingLeaseViewModel
{
    public required string HolderName { get; init; }
    public required int LeaseApplicationId { get; init; }
    public required string PropertyName { get; init; }
    public required string UnitNumber { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
}
