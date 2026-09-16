using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.Infrastructure;

public static class StatusBadges
{
    public static IReadOnlyDictionary<string, string> ByName { get; } =
        Enum.GetValues<ApplicationStatus>().ToDictionary(s => s.ToString(), CssClass);

    public static string CssClass(ApplicationStatus status)
    {
        switch (status)
        {
            case ApplicationStatus.Draft:
                return "status-badge status-draft";
            case ApplicationStatus.Submitted:
                return "status-badge status-submitted";
            case ApplicationStatus.UnderReview:
                return "status-badge status-under-review";
            case ApplicationStatus.Returned:
                return "status-badge status-returned";
            case ApplicationStatus.Approved:
                return "status-badge status-approved";
            case ApplicationStatus.Denied:
                return "status-badge status-denied";
            case ApplicationStatus.Withdrawn:
                return "status-badge status-withdrawn";
            default:
                return "status-badge";
        }
    }
}
