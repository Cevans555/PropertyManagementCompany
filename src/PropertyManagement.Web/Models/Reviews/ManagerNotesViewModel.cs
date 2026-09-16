namespace PropertyManagement.Web.Models.Reviews;

public sealed record ManagerNotesViewModel(int ApplicationId, IReadOnlyList<ManagerNoteRowViewModel> Notes);
