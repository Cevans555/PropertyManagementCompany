using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Data.Services;
using PropertyManagement.Web.Authorization;
using PropertyManagement.Web.Infrastructure;
using PropertyManagement.Web.Models.Applications.Forms;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.Controllers;

/// <summary>
/// The people on an application: the list that refreshes in place and the modal that adds a co-applicant.
/// Anyone already on the application may add another, and the ownership checks apply to all of them.
/// </summary>
[Authorize]
public class ApplicationApplicantsController : Controller
{
    private const string CoApplicantFormPartial = "_CoApplicantForm";

    private readonly RentalApplicationService _applicationService;
    private readonly ApplicationPageBuilder _pageBuilder;

    public ApplicationApplicantsController(RentalApplicationService applicationService, ApplicationPageBuilder pageBuilder)
    {
        _applicationService = applicationService;
        _pageBuilder = pageBuilder;
    }

    [HttpGet]
    public async Task<IActionResult> List(int id, CancellationToken cancellationToken)
    {
        var access = await _pageBuilder.AuthorizeAsync(User, id, ApplicationOperations.View, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        var editable = await _pageBuilder.IsAllowedAsync(User, access.Application, ApplicationOperations.Edit);
        return ViewComponent("ApplicantList", new { applicationId = id, editable });
    }

    [HttpGet]
    public async Task<IActionResult> Add(int applicationId, CancellationToken cancellationToken)
    {
        var access = await _pageBuilder.AuthorizeAsync(User, applicationId, ApplicationOperations.Edit, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        return PartialView(CoApplicantFormPartial, new CoApplicantFormViewModel { ApplicationId = applicationId });
    }

    [HttpPost]
    public async Task<IActionResult> Add(int applicationId, CoApplicantFormViewModel model, CancellationToken cancellationToken)
    {
        var access = await _pageBuilder.AuthorizeAsync(User, applicationId, ApplicationOperations.Edit, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        model.ApplicationId = applicationId;
        if (!ModelState.IsValid)
            return this.ModalInvalid(CoApplicantFormPartial, model);

        var result = await _applicationService.AddCoApplicantAsync(applicationId, User.Id(), model.Email, cancellationToken);
        return this.ToModalResult(result, CoApplicantFormPartial, model);
    }
}
