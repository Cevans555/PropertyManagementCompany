using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Queries;

namespace PropertyManagement.Web.ViewComponents;

public class AvailableUnitsViewComponent : ViewComponent
{
    private readonly UnitQueries _queries;

    public AvailableUnitsViewComponent(UnitQueries queries)
    {
        _queries = queries;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        return View(await _queries.AvailableAsync(userId, HttpContext.RequestAborted));
    }
}
