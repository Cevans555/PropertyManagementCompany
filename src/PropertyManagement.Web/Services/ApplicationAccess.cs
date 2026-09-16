using PropertyManagement.Core.Entities;

namespace PropertyManagement.Web.Services;

public sealed record ApplicationAccess
{
    public RentalApplication? Application { get; }

    public ApplicationAccessOutcome Outcome { get; }

    private ApplicationAccess(RentalApplication? application, ApplicationAccessOutcome outcome)
    {
        Application = application;
        Outcome = outcome;
    }

    public static ApplicationAccess Allowed(RentalApplication application)
    {
        return new ApplicationAccess(application, ApplicationAccessOutcome.Allowed);
    }

    public static ApplicationAccess NotFound()
    {
        return new ApplicationAccess(null, ApplicationAccessOutcome.NotFound);
    }

    public static ApplicationAccess Forbidden()
    {
        return new ApplicationAccess(null, ApplicationAccessOutcome.Forbidden);
    }
}
