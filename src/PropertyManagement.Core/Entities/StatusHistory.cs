using PropertyManagement.Core.Common;
using PropertyManagement.Core.Enums;

namespace PropertyManagement.Core.Entities;

/// <summary>Append-only record of a status change and its review comment.</summary>
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

    /// <summary>For changes that must explain themselves (Return, Deny).</summary>
    internal static string RequiredComment(string? comment)
    {
        return Guard.Required(comment, "Comment", CommentMaxLength);
    }

    /// <summary>For changes where a comment is optional (Approve). Blank becomes null.</summary>
    internal static string? OptionalComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return null;

        return Guard.Required(comment, "Comment", CommentMaxLength);
    }
}
