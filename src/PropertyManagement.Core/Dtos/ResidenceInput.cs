namespace PropertyManagement.Core.Dtos;

public sealed class ResidenceInput
{
    public required string Street { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string PostalCode { get; init; }
    public required string LandlordName { get; init; }
    public required string LandlordPhone { get; init; }
    public required DateOnly MoveInDate { get; init; }
    public required DateOnly MoveOutDate { get; init; }
}
