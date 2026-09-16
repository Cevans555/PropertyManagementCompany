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
                return "text-bg-secondary";
            case ApplicationStatus.Submitted:
                return "text-bg-primary";
            case ApplicationStatus.UnderReview:
                return "text-bg-info";
            case ApplicationStatus.Returned:
                return "text-bg-warning";
            case ApplicationStatus.Approved:
                return "text-bg-success";
            case ApplicationStatus.Denied:
                return "text-bg-danger";
            default:
                return "text-bg-light border";
        }
    }
}
