namespace PropertyManagement.Web.Models;

public sealed record DeleteConfirmViewModel(
    string Title,
    string Message,
    string PostUrl,
    string ConfirmText = "Remove",
    IReadOnlyDictionary<string, string>? HiddenFields = null);
