namespace PropertyManagement.Web.Models.Reviews;

public sealed record ManagerNoteRowViewModel
{
    public required int Id { get; init; }
    public required string Text { get; init; }
    public required string Author { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
    public string? ModifiedBy { get; init; }
}
