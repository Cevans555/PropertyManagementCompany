using PropertyManagement.Core.ValueObjects;

namespace PropertyManagement.Core.Dtos;

public sealed record ResidenceDetails
{
    public required Address Address { get; init; }
    public required string LandlordName { get; init; }
    public required string LandlordPhone { get; init; }
    public required DateOnly MoveInDate { get; init; }
    public required DateOnly MoveOutDate { get; init; }
}