namespace PropertyManagement.Core.Dtos;

public sealed record PropertyDetails
{
    public required string Name { get; init; }
    public required string Street { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string PostalCode { get; init; }
}
