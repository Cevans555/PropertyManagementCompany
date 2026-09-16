using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Web.Models.Applications;
using PropertyManagement.Web.Queries;

namespace PropertyManagement.Web.ViewComponents;

public class ResidenceListViewComponent : ViewComponent
{
    private readonly ApplicationSectionQueries _queries;

    public ResidenceListViewComponent(ApplicationSectionQueries queries)
    {
        _queries = queries;
    }

    public async Task<IViewComponentResult> InvokeAsync(int applicationId, bool editable)
    {
        var cancellationToken = HttpContext.RequestAborted;

        return View(new ResidenceListViewModel
        {
            ApplicationId = applicationId,
            Editable = editable,
            SectionVersion = await _queries.ResidenceSectionVersionAsync(applicationId, cancellationToken),
            Residences = await _queries.ResidencesAsync(applicationId, cancellationToken)
        });
    }
}
