using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Enums;
using PropertyManagement.Data.Queries;
using PropertyManagement.Data.Services;
using PropertyManagement.Web.Authorization;
using PropertyManagement.Web.Infrastructure;
using PropertyManagement.Web.Models;
using PropertyManagement.Web.Models.Reviews;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.Controllers;

[Authorize(Policy = Policies.PropertyManager)]
public class ReviewsController : Controller
{
    private const string ReviewFormPartial = "_ReviewForm";
    private const string NoteFormPartial = "_NoteForm";
    private const string ConfirmPartial = "_DeleteConfirm";

    private readonly ManagerNoteQueries _notes;
    private readonly ApplicationReviewService _reviewService;
    private readonly ManagerNoteService _noteService;
    private readonly ApplicationPageBuilder _pageBuilder;
    private readonly TimeProvider _timeProvider;

    public ReviewsController(
        ManagerNoteQueries notes,
        ApplicationReviewService reviewService,
        ManagerNoteService noteService,
        ApplicationPageBuilder pageBuilder,
        TimeProvider timeProvider)
    {
        _notes = notes;
        _reviewService = reviewService;
        _noteService = noteService;
        _pageBuilder = pageBuilder;
        _timeProvider = timeProvider;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private DateOnly Today => _timeProvider.Today();

    [HttpGet]
    public IActionResult Queue()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Claim(int id, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(id, ApplicationOperations.Review, cancellationToken);
        if (application is null)
            return denied!;

        var result = await _reviewService.ClaimAsync(id, CurrentUserId, cancellationToken);
        if (!result.Succeeded)
        {
            TempData[ApplicationsController.ErrorMessageKey] = result.Error;
            return RedirectToAction(nameof(Queue));
        }

        TempData[ApplicationsController.StatusMessageKey] = "You claimed this application for review.";
        return RedirectToAction(nameof(ApplicationsController.Details), "Applications", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Release(int id, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(id, ApplicationOperations.Review, cancellationToken);
        if (application is null)
            return denied!;

        var result = await _reviewService.ReleaseAsync(id, CurrentUserId, cancellationToken);
        if (!result.Succeeded)
        {
            TempData[ApplicationsController.ErrorMessageKey] = result.Error;
            return RedirectToAction(nameof(ApplicationsController.Details), "Applications", new { id });
        }

        TempData[ApplicationsController.StatusMessageKey] = "The application is back in the review queue.";
        return RedirectToAction(nameof(Queue));
    }

    [HttpGet]
    public async Task<IActionResult> Review(int id, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(id, ApplicationOperations.Review, cancellationToken);
        if (application is null)
            return denied!;

        var model = NewReviewForm(application);
        model.LeaseStartDate = Today;

        if (application.Status != ApplicationStatus.UnderReview || application.ClaimedById != CurrentUserId)
        {
            ModelState.AddModelError(string.Empty, "Claim this application before completing a review.");
            return this.ModalInvalid(ReviewFormPartial, model);
        }

        return PartialView(ReviewFormPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> Review(int id, ReviewFormViewModel model, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(id, ApplicationOperations.Review, cancellationToken);
        if (application is null)
            return denied!;

        var form = NewReviewForm(application);
        form.Outcome = model.Outcome;
        form.Comment = model.Comment;
        form.LeaseStartDate = model.LeaseStartDate;

        if (!ModelState.IsValid)
            return this.ModalInvalid(ReviewFormPartial, form);

        var managerId = CurrentUserId;
        var result = model.Outcome switch
        {
            ReviewOutcome.Approve => await _reviewService.ApproveAsync(id, managerId, model.LeaseStartDate!.Value, model.Comment, cancellationToken),
            ReviewOutcome.Return => await _reviewService.ReturnAsync(id, managerId, model.Comment!, cancellationToken),
            ReviewOutcome.Deny => await _reviewService.DenyAsync(id, managerId, model.Comment!, cancellationToken),
            _ => ServiceResult.Failure(ReviewFormViewModel.InvalidOutcomeMessage)
        };

        return ToModalResult(result, ReviewFormPartial, form);
    }

    [HttpGet]
    public async Task<IActionResult> Notes(int id, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(id, ApplicationOperations.ManageNotes, cancellationToken);
        if (application is null)
            return denied!;

        return ViewComponent("ManagerNotes", new { applicationId = id });
    }

    [HttpGet]
    public async Task<IActionResult> AddNote(int applicationId, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(applicationId, ApplicationOperations.ManageNotes, cancellationToken);
        if (application is null)
            return denied!;

        return PartialView(NoteFormPartial, new NoteFormViewModel { ApplicationId = applicationId });
    }

    [HttpPost]
    public async Task<IActionResult> AddNote(int applicationId, NoteFormViewModel model, CancellationToken cancellationToken)
    {
        var (application, denied) = await LoadAuthorizedAsync(applicationId, ApplicationOperations.ManageNotes, cancellationToken);
        if (application is null)
            return denied!;

        model.ApplicationId = applicationId;
        if (!ModelState.IsValid)
            return this.ModalInvalid(NoteFormPartial, model);

        var result = await _noteService.AddAsync(applicationId, model.Text, cancellationToken);
        return ToModalResult(result, NoteFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> EditNote(int id, CancellationToken cancellationToken)
    {
        var note = await _notes.FindAsync(id, cancellationToken);
        if (note is null)
            return NotFound();

        var (application, denied) = await LoadAuthorizedAsync(note.RentalApplicationId, ApplicationOperations.ManageNotes, cancellationToken);
        if (application is null)
            return denied!;

        var model = new NoteFormViewModel { Id = id, ApplicationId = note.RentalApplicationId, Text = note.Text };
        return PartialView(NoteFormPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> EditNote(int id, NoteFormViewModel model, CancellationToken cancellationToken)
    {
        var applicationId = await _notes.ApplicationIdAsync(id, cancellationToken);
        if (applicationId is null)
            return NotFound();

        var (application, denied) = await LoadAuthorizedAsync(applicationId.Value, ApplicationOperations.ManageNotes, cancellationToken);
        if (application is null)
            return denied!;

        model.Id = id;
        model.ApplicationId = applicationId.Value;
        if (!ModelState.IsValid)
            return this.ModalInvalid(NoteFormPartial, model);

        var result = await _noteService.EditAsync(id, model.Text, cancellationToken);
        return ToModalResult(result, NoteFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> DeleteNote(int id, CancellationToken cancellationToken)
    {
        var applicationId = await _notes.ApplicationIdAsync(id, cancellationToken);
        if (applicationId is null)
            return NotFound();

        var (application, denied) = await LoadAuthorizedAsync(applicationId.Value, ApplicationOperations.ManageNotes, cancellationToken);
        if (application is null)
            return denied!;

        return PartialView(ConfirmPartial, DeleteNoteConfirmation(id));
    }

    [HttpPost]
    [ActionName(nameof(DeleteNote))]
    public async Task<IActionResult> DeleteNoteConfirmed(int id, CancellationToken cancellationToken)
    {
        var applicationId = await _notes.ApplicationIdAsync(id, cancellationToken);
        if (applicationId is null)
            return NotFound();

        var (application, denied) = await LoadAuthorizedAsync(applicationId.Value, ApplicationOperations.ManageNotes, cancellationToken);
        if (application is null)
            return denied!;

        var result = await _noteService.RemoveAsync(id, cancellationToken);
        return ToModalResult(result, ConfirmPartial, DeleteNoteConfirmation(id));
    }

    private async Task<(RentalApplication? Application, IActionResult? Denied)> LoadAuthorizedAsync(
        int applicationId, OperationAuthorizationRequirement operation, CancellationToken cancellationToken)
    {
        var application = await _pageBuilder.LoadAsync(applicationId, cancellationToken);
        if (application is null)
            return (null, NotFound());

        if (!await _pageBuilder.IsAllowedAsync(User, application, operation))
            return (null, Forbid());

        return (application, null);
    }

    private ReviewFormViewModel NewReviewForm(RentalApplication application)
    {
        return new ReviewFormViewModel
        {
            ApplicationId = application.Id,
            UnitLabel = $"{application.Unit.Property.Name} - Unit {application.Unit.UnitNumber}",
            Today = Today
        };
    }

    private DeleteConfirmViewModel DeleteNoteConfirmation(int noteId)
    {
        return new DeleteConfirmViewModel
        {
            Title = "Remove note",
            Message = "Remove this note? This can't be undone.",
            PostUrl = Url.Action(nameof(DeleteNote), new { id = noteId })!
        };
    }

    private IActionResult ToModalResult(ServiceResult result, string partialViewName, object model)
    {
        if (result.Succeeded)
            return this.ModalSuccess();

        ModelState.AddModelError(string.Empty, result.Error!);
        return this.ModalInvalid(partialViewName, model);
    }
}
