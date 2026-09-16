namespace PropertyManagement.Web.Models.Reviews;

public sealed record ManagerNoteRowViewModel(
    int Id,
    string Text,
    string Author,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    string? ModifiedBy);
