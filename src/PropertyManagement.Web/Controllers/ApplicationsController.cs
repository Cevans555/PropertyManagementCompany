using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Entities;
using PropertyManagement.Data;
using PropertyManagement.Data.Services;
using PropertyManagement.Web.Authorization;
using PropertyManagement.Web.Infrastructure;
using PropertyManagement.Web.Models;
using PropertyManagement.Web.Models.Applications;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.Controllers;

[Authorize]
public class ApplicationsController : Controller
{
    public const string StatusMessageKey = "StatusMessage";
    public const string ErrorMessageKey = "ErrorMessage";

    private const string ResidenceFormPartial = "_ResidenceForm";
    private const string CoApplicantFormPartial = "_CoApplicantForm";
    private const string ConfirmPartial = "_DeleteConfirm";

    private readonly PropertyManagementDbContext _db;
    private readonly RentalApplicationService _applicationService;
    private readonly ApplicationPageBuilder _pageBuilder;

    public ApplicationsController(
        PropertyManagementDbContext db,
        RentalApplicationService applicationService,
        ApplicationPageBuilder pageBuilder)
    {
        _db = db;
        _applicationService = applicationService;
        _pageBuilder = pageBuilder;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

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
        var userId = CurrentUserId;

        var existingId = await _db.RentalApplications
            .Where(a => a.UnitId == unitId
                && a.Applicants.Any(p => p.UserId == userId)
                && a.Status != Core.Enums.ApplicationStatus.Approved
                && a.Status != Core.Enums.ApplicationStatus.Denied
                && a.Status != Core.Enums.ApplicationStatus.Withdrawn)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync(cancellationToken);

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
        var (application, denied) = await LoadAuthorizedAsync(id, ApplicationOperations.View, cancellationToken);
        if (application is null)
            return denied!;

        var canEdit = await _pageBuilder.IsAllowedAsync(User, application, ApplicationOperations.Edit);
        var defaultSection = canEdit ? ApplicationSection.Applicant : ApplicationSection.Summary;

        var page = await _pageBuilder.BuildAsync(application, section ?? defaultSection, User, posted: null, cancellationToken);
        return View(page);
    }

    /// <summary>
    /// The application form's single POST. The clicked button decides what happens: Continue validates and saves
    /// the current section, Back never saves, and Submit only works from the Summary.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Details(int id, ApplicationPageViewModel model, string? command, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(id, ApplicationOperations.Edit, cancellationToken);
        if (application is null)
            return denied!;

        var userId = CurrentUserId;
        switch (command)
        {
            case ApplicationCommands.Back:
                return RedirectToSection(id, model.Section.Previous());

            case ApplicationCommands.Continue when model.Section == ApplicationSection.Applicant:
            {
                KeepModelStateFor(nameof(model.ApplicantDetails));
                if (!ModelState.IsValid)
                    return await RedisplayAsync(application, model, error: null, cancellationToken);

                var result = await _applicationService.SaveApplicantDetailsAsync(
                    id, userId, model.ApplicantDetails.ToDetails(), DecodeRowVersion(model.ApplicantRowVersion), cancellationToken);
                if (!result.Succeeded)
                    return await RedisplayAsync(application, model, result.Error, cancellationToken);

                return RedirectToSection(id, ApplicationSection.Residences);
            }

            case ApplicationCommands.Continue when model.Section == ApplicationSection.Residences:
            {
                ModelState.Clear();
                var result = await _applicationService.SaveResidenceSectionAsync(
                    id, userId, model.ResidenceSectionVersion, cancellationToken);
                if (!result.Succeeded)
                    return await RedisplayAsync(application, model, result.Error, cancellationToken);

                return RedirectToSection(id, ApplicationSection.Summary);
            }

            case ApplicationCommands.Submit when model.Section == ApplicationSection.Summary:
            {
                ModelState.Clear();
                var result = await _applicationService.SubmitAsync(id, userId, cancellationToken);
                if (!result.Succeeded)
                    return await RedisplayAsync(application, model, result.Error, cancellationToken);

                TempData[StatusMessageKey] = "Your application was submitted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            default:
                return BadRequest();
        }
    }

    [HttpGet]
    public async Task<IActionResult> ResidenceList(int id, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(id, ApplicationOperations.View, cancellationToken);
        if (application is null)
            return denied!;

        var editable = await _pageBuilder.IsAllowedAsync(User, application, ApplicationOperations.Edit);
        return ViewComponent("ResidenceList", new { applicationId = id, editable });
    }

    [HttpGet]
    public async Task<IActionResult> ApplicantList(int id, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(id, ApplicationOperations.View, cancellationToken);
        if (application is null)
            return denied!;

        var editable = await _pageBuilder.IsAllowedAsync(User, application, ApplicationOperations.Edit);
        return ViewComponent("ApplicantList", new { applicationId = id, editable });
    }

    [HttpGet]
    public async Task<IActionResult> CreateResidence(int applicationId, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(applicationId, ApplicationOperations.Edit, cancellationToken);
        if (application is null)
            return denied!;

        var model = new ResidenceFormViewModel
        {
            ApplicationId = applicationId,
            ResidenceSectionVersion = application.ResidenceSectionVersion
        };

        return PartialView(ResidenceFormPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateResidence(int applicationId, ResidenceFormViewModel model, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(applicationId, ApplicationOperations.Edit, cancellationToken);
        if (application is null)
            return denied!;

        model.ApplicationId = applicationId;
        if (!ModelState.IsValid)
            return this.ModalInvalid(ResidenceFormPartial, model);

        var result = await _applicationService.AddResidenceAsync(
            applicationId, CurrentUserId, model.ToInput(), model.ResidenceSectionVersion, cancellationToken);
        return ToModalResult(result, ResidenceFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> EditResidence(int id, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedForResidenceAsync(id, cancellationToken);
        if (application is null)
            return denied!;

        var residence = application.Residences.Single(r => r.Id == id);
        return PartialView(ResidenceFormPartial, ResidenceFormViewModel.From(residence, application.Id, application.ResidenceSectionVersion));
    }

    [HttpPost]
    public async Task<IActionResult> EditResidence(int id, ResidenceFormViewModel model, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedForResidenceAsync(id, cancellationToken);
        if (application is null)
            return denied!;

        model.Id = id;
        model.ApplicationId = application.Id;
        if (!ModelState.IsValid)
            return this.ModalInvalid(ResidenceFormPartial, model);

        var result = await _applicationService.UpdateResidenceAsync(
            application.Id, CurrentUserId, id, model.ToInput(), model.ResidenceSectionVersion, cancellationToken);
        return ToModalResult(result, ResidenceFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> DeleteResidence(int id, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedForResidenceAsync(id, cancellationToken);
        if (application is null)
            return denied!;

        return PartialView(ConfirmPartial, RemoveResidenceConfirmation(application, id, application.ResidenceSectionVersion));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteResidence(int id, Guid residenceSectionVersion, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedForResidenceAsync(id, cancellationToken);
        if (application is null)
            return denied!;

        var result = await _applicationService.RemoveResidenceAsync(
            application.Id, CurrentUserId, id, residenceSectionVersion, cancellationToken);
        return ToModalResult(result, ConfirmPartial, RemoveResidenceConfirmation(application, id, residenceSectionVersion));
    }

    [HttpGet]
    public async Task<IActionResult> AddCoApplicant(int applicationId, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(applicationId, ApplicationOperations.Edit, cancellationToken);
        if (application is null)
            return denied!;

        return PartialView(CoApplicantFormPartial, new CoApplicantFormViewModel { ApplicationId = applicationId });
    }

    [HttpPost]
    public async Task<IActionResult> AddCoApplicant(int applicationId, CoApplicantFormViewModel model, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(applicationId, ApplicationOperations.Edit, cancellationToken);
        if (application is null)
            return denied!;

        model.ApplicationId = applicationId;
        if (!ModelState.IsValid)
            return this.ModalInvalid(CoApplicantFormPartial, model);

        var result = await _applicationService.AddCoApplicantAsync(applicationId, CurrentUserId, model.Email, cancellationToken);
        return ToModalResult(result, CoApplicantFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> Withdraw(int id, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(id, ApplicationOperations.Withdraw, cancellationToken);
        if (application is null)
            return denied!;

        return PartialView(ConfirmPartial, WithdrawConfirmation(id));
    }

    [HttpPost]
    [ActionName(nameof(Withdraw))]
    public async Task<IActionResult> WithdrawConfirmed(int id, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(id, ApplicationOperations.Withdraw, cancellationToken);
        if (application is null)
            return denied!;

        var result = await _applicationService.WithdrawAsync(id, CurrentUserId, cancellationToken);
        return ToModalResult(result, ConfirmPartial, WithdrawConfirmation(id));
    }

    /// <summary>
    /// An application the user can't view is reported as 404 so ids can't be probed; one they can view but not
    /// change this way is 403.
    /// </summary>
    private async Task<(RentalApplication? Application, IActionResult? Denied)> LoadAuthorizedAsync(
        int applicationId, OperationAuthorizationRequirement operation, CancellationToken cancellationToken)
    {
        var application = await _pageBuilder.LoadAsync(applicationId, cancellationToken);
        if (application is null || !await _pageBuilder.IsAllowedAsync(User, application, ApplicationOperations.View))
            return (null, NotFound());

        if (operation != ApplicationOperations.View && !await _pageBuilder.IsAllowedAsync(User, application, operation))
            return (null, Forbid());

        return (application, null);
    }

    private async Task<(RentalApplication? Application, IActionResult? Denied)> LoadAuthorizedForResidenceAsync(
        int residenceId, CancellationToken cancellationToken)
    {
        var applicationId = await _db.ResidenceHistories
            .Where(r => r.Id == residenceId)
            .Select(r => (int?)r.RentalApplicationId)
            .SingleOrDefaultAsync(cancellationToken);

        if (applicationId is null)
            return (null, NotFound());

        return await LoadAuthorizedAsync(applicationId.Value, ApplicationOperations.Edit, cancellationToken);
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

    /// <summary>Only the current section is validated; the form doesn't post the other sections' fields.</summary>
    private void KeepModelStateFor(string prefix)
    {
        foreach (var key in ModelState.Keys.Where(k => !k.StartsWith(prefix + ".", StringComparison.Ordinal)).ToList())
            ModelState.Remove(key);
    }

    private IActionResult RedirectToSection(int id, ApplicationSection section)
    {
        return RedirectToAction(nameof(Details), new { id, section });
    }

    private IActionResult ToModalResult(ServiceResult result, string partialViewName, object model)
    {
        if (result.Succeeded)
            return this.ModalSuccess();

        ModelState.AddModelError(string.Empty, result.Error!);
        return this.ModalInvalid(partialViewName, model);
    }

    private DeleteConfirmViewModel RemoveResidenceConfirmation(RentalApplication application, int residenceId, Guid sectionVersion)
    {
        var residence = application.Residences.SingleOrDefault(r => r.Id == residenceId);
        var message = residence is null
            ? "Remove this residence?"
            : $"Remove {residence.Address} from your residence history?";

        return new DeleteConfirmViewModel(
            "Remove residence",
            message,
            Url.Action(nameof(DeleteResidence), new { id = residenceId })!,
            HiddenFields: new Dictionary<string, string>
            {
                [nameof(ApplicationPageViewModel.ResidenceSectionVersion)] = sectionVersion.ToString()
            });
    }

    private DeleteConfirmViewModel WithdrawConfirmation(int id)
    {
        return new DeleteConfirmViewModel(
            "Withdraw application",
            "Withdraw this application? You won't be able to change or resubmit it.",
            Url.Action(nameof(Withdraw), new { id })!,
            ConfirmText: "Withdraw application");
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
