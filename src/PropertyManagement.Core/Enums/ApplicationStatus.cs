namespace PropertyManagement.Core.Enums;

public enum ApplicationStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    Returned = 4,
    Approved = 5,
    Denied = 6,
    Withdrawn = 7
}

public static class ApplicationStatusExtensions
{
    public static bool IsTerminal(this ApplicationStatus status) =>
        status is ApplicationStatus.Approved or ApplicationStatus.Denied or ApplicationStatus.Withdrawn;

    public static string DisplayName(this ApplicationStatus status) => status switch
    {
        ApplicationStatus.UnderReview => "Under Review",
        _ => status.ToString()
    };
}