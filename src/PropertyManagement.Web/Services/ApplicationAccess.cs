using PropertyManagement.Core.Entities;

namespace PropertyManagement.Web.Services;

/// <summary>
/// The outcome of loading an application for a user. <see cref="Application"/> is set only when the operation is
/// allowed, so a caller can treat null as "refused" and use <see cref="Outcome"/> to say why. Hiding an application
/// the user can't even view behind "not found" keeps its existence private.
/// </summary>
public sealed record ApplicationAccess(RentalApplication? Application, ApplicationAccessOutcome Outcome)
{
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
