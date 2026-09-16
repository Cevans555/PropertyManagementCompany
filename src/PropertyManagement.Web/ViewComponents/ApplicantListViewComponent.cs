using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Models.Applications;
using PropertyManagement.Web.Queries;

namespace PropertyManagement.Web.ViewComponents;

public class ApplicantListViewComponent : ViewComponent
{
    private readonly ApplicationSectionQueries _queries;

    public ApplicantListViewComponent(ApplicationSectionQueries queries)
    {
        _queries = queries;
    }

    public async Task<IViewComponentResult> InvokeAsync(int applicationId, bool editable)
    {
        var people = await _queries.ApplicantsAsync(applicationId, HttpContext.RequestAborted);

        return View(new ApplicantListViewModel
        {
            ApplicationId = applicationId,
            Editable = editable,
            People = people
        });
    }
}
