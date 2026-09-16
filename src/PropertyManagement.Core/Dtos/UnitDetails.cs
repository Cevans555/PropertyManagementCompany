namespace PropertyManagement.Core.Dtos;

public sealed record UnitDetails
{
    public required string UnitNumber { get; init; }
    public required int Bedrooms { get; init; }
    public required decimal MonthlyRent { get; init; }
    public required int UnitTypeId { get; init; }
}
