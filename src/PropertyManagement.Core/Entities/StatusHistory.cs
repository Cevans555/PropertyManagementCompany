using PropertyManagement.Core.Enums;

namespace PropertyManagement.Core.Entities;

public class StatusHistory
{
    public const int CommentMaxLength = 1000;

    public int Id { get; private set; }
    public int RentalApplicationId { get; private set; }
    public ApplicationStatus? FromStatus { get; private set; }
    public ApplicationStatus ToStatus { get; private set; }
    public string ChangedById { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public string? Comment { get; private set; }

    private StatusHistory()
    {
        ChangedById = null!;
    }

    internal StatusHistory(ApplicationStatus? fromStatus, ApplicationStatus toStatus, string changedById, DateTime changedAt, string? comment)
    {
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedById = changedById;
        ChangedAt = changedAt;
        Comment = comment;
    }
}
