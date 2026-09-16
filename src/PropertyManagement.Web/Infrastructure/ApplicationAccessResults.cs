using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.Infrastructure;

public static class ApplicationAccessResults
{
    public static IActionResult DeniedResult(this Controller controller, ApplicationAccess access)
    {
        if (access.Outcome == ApplicationAccessOutcome.Forbidden)
            return controller.Forbid();

        return controller.NotFound();
    }
}
