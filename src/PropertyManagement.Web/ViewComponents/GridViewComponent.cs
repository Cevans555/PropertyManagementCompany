using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Models.Grid;

namespace PropertyManagement.Web.ViewComponents;

public class GridViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(GridViewModel grid)
    {
        return View(grid);
    }
}
