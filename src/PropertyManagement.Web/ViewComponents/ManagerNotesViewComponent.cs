using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Models.Reviews;
using PropertyManagement.Web.Queries;

namespace PropertyManagement.Web.ViewComponents;

public class ManagerNotesViewComponent : ViewComponent
{
    private readonly ReviewQueries _queries;

    public ManagerNotesViewComponent(ReviewQueries queries)
    {
        _queries = queries;
    }

    public async Task<IViewComponentResult> InvokeAsync(int applicationId)
    {
        return View(new ManagerNotesViewModel
        {
            ApplicationId = applicationId,
            Notes = await _queries.NotesAsync(applicationId, HttpContext.RequestAborted)
        });
    }
}
