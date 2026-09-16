namespace PropertyManagement.Web.Models.Applications;

public sealed record ResidenceRowViewModel
{
    public required int Id { get; init; }
    public required string Address { get; init; }
    public required string LandlordName { get; init; }
    public required string LandlordPhone { get; init; }
    public required DateOnly MoveInDate { get; init; }
    public required DateOnly MoveOutDate { get; init; }
}
