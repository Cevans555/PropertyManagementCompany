using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.Infrastructure;

public static class ApplicationAccessResults
{
    /// <summary>The response to send when access was refused. Only call this when the access wasn't allowed.</summary>
    public static IActionResult DeniedResult(this Controller controller, ApplicationAccess access)
    {
        if (access.Outcome == ApplicationAccessOutcome.Forbidden)
            return controller.Forbid();

        return controller.NotFound();
    }
}
