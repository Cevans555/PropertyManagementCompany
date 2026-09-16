namespace PropertyManagement.Web.Models.Properties;

public sealed record PropertyHeaderViewModel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Street { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string PostalCode { get; init; }
}
