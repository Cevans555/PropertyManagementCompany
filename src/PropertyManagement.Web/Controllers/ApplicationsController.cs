using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Enums;
using PropertyManagement.Core.Security;
using PropertyManagement.Core.Validation;
using PropertyManagement.Data.Queries;
using PropertyManagement.Data.Services;
using PropertyManagement.Web.Authorization;
using PropertyManagement.Web.Infrastructure;
using PropertyManagement.Web.Models;
using PropertyManagement.Web.Models.Applications;
using PropertyManagement.Web.Models.Applications.Page;
using PropertyManagement.Web.Models.Applications.List;
using PropertyManagement.Web.Queries;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.Controllers;

[Authorize]
public class ApplicationsController : Controller
{
    public const string StatusMessageKey = "StatusMessage";
    public const string WarningMessageKey = "WarningMessage";
    public const string ErrorMessageKey = "ErrorMessage";

    private const string ConfirmPartial = "_DeleteConfirm";

    private readonly ApplicationQueries _applications;
    private readonly PropertyQueries _properties;
    private readonly RentalApplicationService _applicationService;
    private readonly ApplicationPageBuilder _pageBuilder;
    private readonly IOptionsSnapshot<FeatureOptions> _features;

    public ApplicationsController(
        ApplicationQueries applications,
        PropertyQueries properties,
        RentalApplicationService applicationService,
        ApplicationPageBuilder pageBuilder,
        IOptionsSnapshot<FeatureOptions> features)
    {
        _applications = applications;
        _properties = properties;
        _applicationService = applicationService;
        _pageBuilder = pageBuilder;
        _features = features;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ApplicationStatus? status, int? propertyId, CancellationToken cancellationToken)
    {
        var statusOptions = Enum.GetValues<ApplicationStatus>()
            .Select(s => new SelectListItem(s.DisplayName(), s.ToString(), s == status))
            .ToList();

        return View(new ApplicationListPageViewModel
        {
            Status = status,
            PropertyId = propertyId,
            IsManager = User.IsInRole(Roles.PropertyManager),
            StatusOptions = statusOptions,
            PropertyOptions = await _properties.OptionsAsync(propertyId, cancellationToken)
        });
    }

    [HttpGet]
    [Authorize(Policy = Policies.Applicant)]
    public IActionResult Available()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Policy = Policies.Applicant)]
    public async Task<IActionResult> Start(int unitId, CancellationToken cancellationToken)
    {
        var userId = User.Id();

        var existingId = await _applications.OpenApplicationIdAsync(unitId, userId, cancellationToken);
        if (existingId is not null)
            return RedirectToAction(nameof(Details), new { id = existingId });

        var result = await _applicationService.StartAsync(unitId, userId, cancellationToken);
        if (!result.Succeeded)
        {
            TempData[ErrorMessageKey] = result.Error;
            return RedirectToAction(nameof(Available));
        }

        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, ApplicationSection? section, CancellationToken cancellationToken)
    {
        var access = await _pageBuilder.AuthorizeAsync(User, id, ApplicationOperations.View, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        var canEdit = await _pageBuilder.IsAllowedAsync(User, access.Application, ApplicationOperations.Edit);
        var defaultSection = canEdit ? ApplicationSection.Applicant : ApplicationSection.Summary;

        var page = await _pageBuilder.BuildAsync(
            access.Application, section ?? defaultSection, User, posted: null, cancellationToken);

        if (page.CanEdit && page.Section == ApplicationSection.Applicant)
        {
            foreach (var error in page.SavedApplicantDetailsErrors)
            {
                ModelState.AddModelError($"{nameof(ApplicationPageViewModel.ApplicantDetails)}.{error.Field}", error.Message);
            }
        }

        return View(page);
    }

    [HttpPost]
    public async Task<IActionResult> Details(int id, ApplicationPageViewModel model, string? command, CancellationToken cancellationToken)
    {
        var access = await _pageBuilder.AuthorizeAsync(User, id, ApplicationOperations.Edit, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        var application = access.Application;
        var userId = User.Id();

        switch (command)
        {
            case ApplicationCommands.Back:
                return RedirectToSection(id, model.Section.Previous());

            case ApplicationCommands.Continue when model.Section == ApplicationSection.Applicant:
                return await ContinueApplicantAsync(id, userId, application, model, cancellationToken);

            case ApplicationCommands.Continue when model.Section == ApplicationSection.Residences:
                return await ContinueResidencesAsync(id, userId, application, model, cancellationToken);

            case ApplicationCommands.Submit when model.Section == ApplicationSection.Summary:
                return await SubmitApplicationAsync(id, userId, application, model, cancellationToken);

            default:
                return BadRequest();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Withdraw(int id, CancellationToken cancellationToken)
    {
        var access = await _pageBuilder.AuthorizeAsync(User, id, ApplicationOperations.Withdraw, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        return PartialView(ConfirmPartial, WithdrawConfirmation(id));
    }

    [HttpPost]
    [ActionName(nameof(Withdraw))]
    public async Task<IActionResult> WithdrawConfirmed(int id, CancellationToken cancellationToken)
    {
        var access = await _pageBuilder.AuthorizeAsync(User, id, ApplicationOperations.Withdraw, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        var result = await _applicationService.WithdrawAsync(id, User.Id(), cancellationToken);
        return this.ToModalResult(result, ConfirmPartial, WithdrawConfirmation(id));
    }

    private async Task<IActionResult> ContinueApplicantAsync(
        int id, string userId, RentalApplication application, ApplicationPageViewModel model, CancellationToken cancellationToken)
    {
        KeepModelStateFor(nameof(model.ApplicantDetails));

        var details = model.ApplicantDetails.ToDetails();
        var allowInvalid = _features.Value.SaveInvalidSections;
        var errors = ApplicantDetailsRules.Validate(details);

        if (!ModelState.IsValid && (!allowInvalid || errors.Any(e => e.BlocksSaving)))
            return await RedisplayAsync(application, model, error: null, cancellationToken);

        var result = await _applicationService.SaveApplicantDetailsAsync(
            id, userId, details, DecodeRowVersion(model.ApplicantRowVersion), allowInvalid, cancellationToken);
        if (!result.Succeeded)
            return await RedisplayAsync(application, model, result.Error, cancellationToken);

        if (errors.Count > 0)
        {
            TempData[WarningMessageKey] =
                "Your applicant information was saved with errors. You can keep going, but you'll need to fix them before you submit.";
        }

        return RedirectToSection(id, ApplicationSection.Residences);
    }

    private async Task<IActionResult> ContinueResidencesAsync(
        int id, string userId, RentalApplication application, ApplicationPageViewModel model, CancellationToken cancellationToken)
    {
        ModelState.Clear();

        var allowInvalid = _features.Value.SaveInvalidSections;
        var result = await _applicationService.SaveResidenceSectionAsync(
            id, userId, model.ResidenceSectionVersion, allowInvalid, cancellationToken);
        if (!result.Succeeded)
            return await RedisplayAsync(application, model, result.Error, cancellationToken);

        if (ResidenceSectionRules.Validate(application.Residences.Count).Count > 0)
        {
            TempData[WarningMessageKey] =
                "Your residence history was saved with errors. You'll need to fix them before you submit.";
        }

        return RedirectToSection(id, ApplicationSection.Summary);
    }

    private async Task<IActionResult> SubmitApplicationAsync(
        int id, string userId, RentalApplication application, ApplicationPageViewModel model, CancellationToken cancellationToken)
    {
        ModelState.Clear();

        var result = await _applicationService.SubmitAsync(id, userId, cancellationToken);
        if (!result.Succeeded)
            return await RedisplayAsync(application, model, result.Error, cancellationToken);

        TempData[StatusMessageKey] = "Your application was submitted.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IActionResult> RedisplayAsync(
        RentalApplication application, ApplicationPageViewModel posted, string? error, CancellationToken cancellationToken)
    {
        if (error is not null)
            ModelState.AddModelError(string.Empty, error);

        var keepInput = posted.Section == ApplicationSection.Applicant ? posted : null;
        var page = await _pageBuilder.BuildAsync(application, posted.Section, User, keepInput, cancellationToken);

        var result = View(nameof(Details), page);
        result.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return result;
    }

    private void KeepModelStateFor(string prefix)
    {
        foreach (var key in ModelState.Keys.Where(k => !k.StartsWith(prefix + ".", StringComparison.Ordinal)).ToList())
            ModelState.Remove(key);
    }

    private IActionResult RedirectToSection(int id, ApplicationSection section)
    {
        return RedirectToAction(nameof(Details), new { id, section });
    }

    private DeleteConfirmViewModel WithdrawConfirmation(int id)
    {
        return new DeleteConfirmViewModel
        {
            Title = "Withdraw application",
            Message = "Withdraw this application? You won't be able to change or resubmit it.",
            PostUrl = Url.Action(nameof(Withdraw), new { id })!,
            ConfirmText = "Withdraw application"
        };
    }

    private static byte[] DecodeRowVersion(string? value)
    {
        try
        {
            return string.IsNullOrEmpty(value) ? [] : Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            return [];
        }
    }
}
