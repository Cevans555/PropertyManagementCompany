using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Models.Units;
using PropertyManagement.Web.Queries;

namespace PropertyManagement.Web.ViewComponents;

public class UnitTableViewComponent : ViewComponent
{
    private readonly UnitQueries _queries;

    public UnitTableViewComponent(UnitQueries queries)
    {
        _queries = queries;
    }

    public async Task<IViewComponentResult> InvokeAsync(int propertyId)
    {
        return View(new UnitTableViewModel
        {
            PropertyId = propertyId,
            Units = await _queries.TableAsync(propertyId, HttpContext.RequestAborted)
        });
    }
}
