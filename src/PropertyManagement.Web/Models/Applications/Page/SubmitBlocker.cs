namespace PropertyManagement.Web.Models.Applications.Page;

public sealed record SubmitBlocker
{
    public required string Message { get; init; }
    public ApplicationSection? FixSection { get; init; }
}
