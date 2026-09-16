namespace PropertyManagement.Core.Enums;

public static class ApplicationStatusExtensions
{
    public static bool IsEditable(this ApplicationStatus status)
    {
        return status == ApplicationStatus.Draft || status == ApplicationStatus.Returned;
    }

    public static bool IsTerminal(this ApplicationStatus status)
    {
        return status is ApplicationStatus.Approved or ApplicationStatus.Denied or ApplicationStatus.Withdrawn;
    }

    public static string DisplayName(this ApplicationStatus status)
    {
        switch (status)
        {
            case ApplicationStatus.UnderReview:
                return "Under Review";
            default:
                return status.ToString();
        }
    }
}
