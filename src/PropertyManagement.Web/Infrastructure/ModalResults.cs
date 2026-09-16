using Microsoft.AspNetCore.Mvc;

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
}
