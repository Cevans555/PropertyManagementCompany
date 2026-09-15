namespace PropertyManagement.Core.Dtos;

public sealed class ApplicantDetails
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }

    // Current address as separate fields instead of the Address value object. Bonus 4 (SaveInvalidSections)
    // lets this section be saved with errors, so it can carry a partial address, which Address refuses to create.
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
}
