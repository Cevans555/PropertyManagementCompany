namespace PropertyManagement.Web.Models;

public sealed record DeleteConfirmViewModel
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string PostUrl { get; init; }
    public string ConfirmText { get; init; } = "Remove";
    public IReadOnlyDictionary<string, string>? HiddenFields { get; init; }
}
