namespace PropertyManagement.Web.Models.Units;

public sealed record UnitRowViewModel
{
    public required int Id { get; init; }
    public required string UnitNumber { get; init; }
    public required string UnitTypeName { get; init; }
    public required bool UnitTypeIsActive { get; init; }
    public required int Bedrooms { get; init; }
    public required decimal MonthlyRent { get; init; }
    public DateOnly? LeasedUntil { get; init; }
    public required int ApplicationCount { get; init; }
}
