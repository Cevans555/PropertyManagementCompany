using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Core.Security;
using PropertyManagement.Web.Models.Applications;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.ViewComponents;

public class ApplicationListViewComponent : ViewComponent
{
    private readonly ApplicationListQuery _query;

    public ApplicationListViewComponent(ApplicationListQuery query)
    {
        _query = query;
    }

    public async Task<IViewComponentResult> InvokeAsync(ApplicationListFilter filter)
    {
        var rows = await _query.ListAsync(UserClaimsPrincipal, filter, HttpContext.RequestAborted);
        return View(new ApplicationListViewModel(rows, UserClaimsPrincipal.IsInRole(Roles.PropertyManager)));
    }
}
