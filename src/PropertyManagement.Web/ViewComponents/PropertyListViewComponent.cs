using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Queries;

namespace PropertyManagement.Web.ViewComponents;

public class PropertyListViewComponent : ViewComponent
{
    private readonly PropertyQueries _queries;

    public PropertyListViewComponent(PropertyQueries queries)
    {
        _queries = queries;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        return View(await _queries.ListAsync(HttpContext.RequestAborted));
    }
}
