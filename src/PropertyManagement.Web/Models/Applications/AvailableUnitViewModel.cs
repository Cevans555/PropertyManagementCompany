namespace PropertyManagement.Web.Models.Applications;

public sealed record AvailableUnitViewModel
{
    public required int UnitId { get; init; }
    public required string PropertyName { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string UnitNumber { get; init; }
    public required string UnitTypeName { get; init; }
    public required int Bedrooms { get; init; }
    public required decimal MonthlyRent { get; init; }
    public int? OpenApplicationId { get; init; }
}
