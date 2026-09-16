using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Queries;

namespace PropertyManagement.Web.ViewComponents;

public class ReviewQueueViewComponent : ViewComponent
{
    private readonly ReviewQueries _queries;

    public ReviewQueueViewComponent(ReviewQueries queries)
    {
        _queries = queries;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var managerId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        return View(await _queries.QueueAsync(managerId, HttpContext.RequestAborted));
    }
}
