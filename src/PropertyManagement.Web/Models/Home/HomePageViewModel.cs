namespace PropertyManagement.Web.Models.Home;

/// <summary>
/// What the landing page shows. The counts are role-specific: a manager sees the size of the review workload,
/// an applicant sees their own applications. A signed-out visitor gets none of them.
/// </summary>
public sealed class HomePageViewModel
{
    public required bool IsAuthenticated { get; init; }

    public required bool IsManager { get; init; }

    public string? DisplayName { get; init; }

    /// <summary>Manager: submitted applications nobody has claimed.</summary>
    public int WaitingToReview { get; init; }

    /// <summary>Manager: applications this manager is currently holding.</summary>
    public int ClaimedByMe { get; init; }

    /// <summary>Manager: properties on file.</summary>
    public int PropertyCount { get; init; }

    /// <summary>Manager: units with no lease covering today.</summary>
    public int AvailableUnitCount { get; init; }

    /// <summary>Applicant: their applications that are a draft or were returned, so they need attention.</summary>
    public int NeedsAttention { get; init; }

    /// <summary>Applicant: their applications waiting on a manager.</summary>
    public int AwaitingDecision { get; init; }

    /// <summary>Applicant: their approved applications.</summary>
    public int Approved { get; init; }
}
