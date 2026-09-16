namespace PropertyManagement.Web.Models.Reviews;

public sealed record ManagerNotesViewModel
{
    public required int ApplicationId { get; init; }
    public required IReadOnlyList<ManagerNoteRowViewModel> Notes { get; init; }
}
