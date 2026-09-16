using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Enums;
using PropertyManagement.Data.Services;
using PropertyManagement.Web.Authorization;
using PropertyManagement.Web.Infrastructure;
using PropertyManagement.Web.Models.Reviews;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.Controllers;

/// <summary>
/// The manager review workflow: the queue, claiming and releasing an application, and completing a review
/// with an outcome of approve, return or deny.
/// </summary>
[Authorize(Policy = Policies.PropertyManager)]
public class ReviewsController : Controller
{
    private const string ReviewFormPartial = "_ReviewForm";

    private readonly ApplicationReviewService _reviewService;
    private readonly ApplicationPageBuilder _pageBuilder;
    private readonly TimeProvider _timeProvider;

    public ReviewsController(
        ApplicationReviewService reviewService,
        ApplicationPageBuilder pageBuilder,
        TimeProvider timeProvider)
    {
        _reviewService = reviewService;
        _pageBuilder = pageBuilder;
        _timeProvider = timeProvider;
    }

    private DateOnly Today => _timeProvider.Today();

    [HttpGet]
    public IActionResult Queue()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Claim(int id, CancellationToken cancellationToken)
    {
        var access = await _pageBuilder.AuthorizeAsync(User, id, ApplicationOperations.Review, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        var result = await _reviewService.ClaimAsync(id, User.Id(), cancellationToken);
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
        var access = await _pageBuilder.AuthorizeAsync(User, id, ApplicationOperations.Review, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        var result = await _reviewService.ReleaseAsync(id, User.Id(), cancellationToken);
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
        var access = await _pageBuilder.AuthorizeAsync(User, id, ApplicationOperations.Review, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        var application = access.Application;
        var model = NewReviewForm(application);
        model.LeaseStartDate = Today;

        if (application.Status != ApplicationStatus.UnderReview || application.ClaimedById != User.Id())
        {
            ModelState.AddModelError(string.Empty, "Claim this application before completing a review.");
            return this.ModalInvalid(ReviewFormPartial, model);
        }

        return PartialView(ReviewFormPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> Review(int id, ReviewFormViewModel model, CancellationToken cancellationToken)
    {
        var access = await _pageBuilder.AuthorizeAsync(User, id, ApplicationOperations.Review, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        var form = NewReviewForm(access.Application);
        form.Outcome = model.Outcome;
        form.Comment = model.Comment;
        form.LeaseStartDate = model.LeaseStartDate;

        if (!ModelState.IsValid)
            return this.ModalInvalid(ReviewFormPartial, form);

        var managerId = User.Id();
        var result = model.Outcome switch
        {
            ReviewOutcome.Approve => await _reviewService.ApproveAsync(id, managerId, model.LeaseStartDate!.Value, model.Comment, cancellationToken),
            ReviewOutcome.Return => await _reviewService.ReturnAsync(id, managerId, model.Comment!, cancellationToken),
            ReviewOutcome.Deny => await _reviewService.DenyAsync(id, managerId, model.Comment!, cancellationToken),
            _ => ServiceResult.Failure(ReviewFormViewModel.InvalidOutcomeMessage)
        };

        return this.ToModalResult(result, ReviewFormPartial, form);
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
}
