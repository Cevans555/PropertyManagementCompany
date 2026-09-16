namespace PropertyManagement.Web.Models.Applications;

public sealed record SubmitBlocker
{
    public required string Message { get; init; }
    public ApplicationSection? FixSection { get; init; }
}
