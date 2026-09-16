using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Data.Services;

namespace PropertyManagement.Web.Infrastructure;

public static class ModalResults
{
    public static PartialViewResult ModalInvalid(this Controller controller, string partialViewName, object model)
    {
        var result = controller.PartialView(partialViewName, model);
        result.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return result;
    }

    public static JsonResult ModalSuccess(this Controller controller)
    {
        return controller.Json(new { success = true });
    }

    /// <summary>Closes the modal on success, or re-renders it with the service's error message.</summary>
    public static IActionResult ToModalResult(
        this Controller controller, ServiceResult result, string partialViewName, object model)
    {
        if (result.Succeeded)
            return controller.ModalSuccess();

        controller.ModelState.AddModelError(string.Empty, result.Error!);
        return controller.ModalInvalid(partialViewName, model);
    }
}
