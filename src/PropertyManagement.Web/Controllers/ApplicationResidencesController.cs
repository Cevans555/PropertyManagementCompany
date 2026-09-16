using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Core.Entities;
using PropertyManagement.Data.Queries;
using PropertyManagement.Data.Services;
using PropertyManagement.Web.Authorization;
using PropertyManagement.Web.Infrastructure;
using PropertyManagement.Web.Models;
using PropertyManagement.Web.Models.Applications.Forms;
using PropertyManagement.Web.Models.Applications.Page;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.Controllers;

[Authorize]
public class ApplicationResidencesController : Controller
{
    private const string ResidenceFormPartial = "_ResidenceForm";
    private const string ConfirmPartial = "_DeleteConfirm";

    private readonly ApplicationQueries _applications;
    private readonly RentalApplicationService _applicationService;
    private readonly ApplicationPageBuilder _pageBuilder;

    public ApplicationResidencesController(
        ApplicationQueries applications,
        RentalApplicationService applicationService,
        ApplicationPageBuilder pageBuilder)
    {
        _applications = applications;
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
        return ViewComponent("ResidenceList", new { applicationId = id, editable });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int applicationId, CancellationToken cancellationToken)
    {
        var access = await _pageBuilder.AuthorizeAsync(User, applicationId, ApplicationOperations.Edit, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        return PartialView(ResidenceFormPartial, new ResidenceFormViewModel
        {
            ApplicationId = applicationId,
            ResidenceSectionVersion = access.Application.ResidenceSectionVersion
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(int applicationId, ResidenceFormViewModel model, CancellationToken cancellationToken)
    {
        var access = await _pageBuilder.AuthorizeAsync(User, applicationId, ApplicationOperations.Edit, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        model.ApplicationId = applicationId;
        if (!ModelState.IsValid)
            return this.ModalInvalid(ResidenceFormPartial, model);

        var result = await _applicationService.AddResidenceAsync(
            applicationId, User.Id(), model.ToInput(), model.ResidenceSectionVersion, cancellationToken);
        return this.ToModalResult(result, ResidenceFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var access = await AuthorizeForResidenceAsync(id, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        var application = access.Application;
        var residence = application.Residences.Single(r => r.Id == id);
        return PartialView(
            ResidenceFormPartial,
            ResidenceFormViewModel.From(residence, application.Id, application.ResidenceSectionVersion));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ResidenceFormViewModel model, CancellationToken cancellationToken)
    {
        var access = await AuthorizeForResidenceAsync(id, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        model.Id = id;
        model.ApplicationId = access.Application.Id;
        if (!ModelState.IsValid)
            return this.ModalInvalid(ResidenceFormPartial, model);

        var result = await _applicationService.UpdateResidenceAsync(
            access.Application.Id, User.Id(), id, model.ToInput(), model.ResidenceSectionVersion, cancellationToken);
        return this.ToModalResult(result, ResidenceFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var access = await AuthorizeForResidenceAsync(id, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        var application = access.Application;
        return PartialView(ConfirmPartial, RemoveConfirmation(application, id, application.ResidenceSectionVersion));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, Guid residenceSectionVersion, CancellationToken cancellationToken)
    {
        var access = await AuthorizeForResidenceAsync(id, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        var application = access.Application;
        var result = await _applicationService.RemoveResidenceAsync(
            application.Id, User.Id(), id, residenceSectionVersion, cancellationToken);
        return this.ToModalResult(result, ConfirmPartial, RemoveConfirmation(application, id, residenceSectionVersion));
    }

    private async Task<ApplicationAccess> AuthorizeForResidenceAsync(int residenceId, CancellationToken cancellationToken)
    {
        var applicationId = await _applications.ApplicationIdForResidenceAsync(residenceId, cancellationToken);
        if (applicationId is null)
            return ApplicationAccess.NotFound();

        return await _pageBuilder.AuthorizeAsync(User, applicationId.Value, ApplicationOperations.Edit, cancellationToken);
    }

    private DeleteConfirmViewModel RemoveConfirmation(RentalApplication application, int residenceId, Guid sectionVersion)
    {
        var residence = application.Residences.SingleOrDefault(r => r.Id == residenceId);
        var message = residence is null
            ? "Remove this residence?"
            : $"Remove {residence.Address} from your residence history?";

        return new DeleteConfirmViewModel
        {
            Title = "Remove residence",
            Message = message,
            PostUrl = Url.Action(nameof(Delete), new { id = residenceId })!,
            HiddenFields = new Dictionary<string, string>
            {
                [nameof(ApplicationPageViewModel.ResidenceSectionVersion)] = sectionVersion.ToString()
            }
        };
    }
}
