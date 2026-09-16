namespace PropertyManagement.Web.Models.Applications;

public static class ApplicationSectionExtensions
{
    public static ApplicationSection Previous(this ApplicationSection section)
    {
        if (section == ApplicationSection.Summary)
            return ApplicationSection.Residences;

        return ApplicationSection.Applicant;
    }

    public static ApplicationSection Next(this ApplicationSection section)
    {
        if (section == ApplicationSection.Applicant)
            return ApplicationSection.Residences;

        return ApplicationSection.Summary;
    }
}
