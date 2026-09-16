using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Data.Queries;
using PropertyManagement.Data.Services;
using PropertyManagement.Web.Authorization;
using PropertyManagement.Web.Infrastructure;
using PropertyManagement.Web.Models;
using PropertyManagement.Web.Models.Reviews;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.Controllers;

/// <summary>
/// Manager notes on an application. A note is its own aggregate rather than part of the application, so it has
/// its own controller and service. Every endpoint is behind the manager policy and the ManageNotes check, so a
/// note is never rendered or returned to an applicant.
/// </summary>
[Authorize(Policy = Policies.PropertyManager)]
public class ManagerNotesController : Controller
{
    private const string NoteFormPartial = "_NoteForm";
    private const string ConfirmPartial = "_DeleteConfirm";

    private readonly ManagerNoteQueries _notes;
    private readonly ManagerNoteService _noteService;
    private readonly ApplicationPageBuilder _pageBuilder;

    public ManagerNotesController(
        ManagerNoteQueries notes,
        ManagerNoteService noteService,
        ApplicationPageBuilder pageBuilder)
    {
        _notes = notes;
        _noteService = noteService;
        _pageBuilder = pageBuilder;
    }

    [HttpGet]
    public async Task<IActionResult> List(int id, CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(id, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        return ViewComponent("ManagerNotes", new { applicationId = id });
    }

    [HttpGet]
    public async Task<IActionResult> Add(int applicationId, CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(applicationId, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        return PartialView(NoteFormPartial, new NoteFormViewModel { ApplicationId = applicationId });
    }

    [HttpPost]
    public async Task<IActionResult> Add(int applicationId, NoteFormViewModel model, CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(applicationId, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        model.ApplicationId = applicationId;
        if (!ModelState.IsValid)
            return this.ModalInvalid(NoteFormPartial, model);

        var result = await _noteService.AddAsync(applicationId, model.Text, cancellationToken);
        return this.ToModalResult(result, NoteFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var note = await _notes.FindAsync(id, cancellationToken);
        if (note is null)
            return NotFound();

        var access = await AuthorizeAsync(note.RentalApplicationId, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        return PartialView(NoteFormPartial, new NoteFormViewModel
        {
            Id = id,
            ApplicationId = note.RentalApplicationId,
            Text = note.Text
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, NoteFormViewModel model, CancellationToken cancellationToken)
    {
        var applicationId = await _notes.ApplicationIdAsync(id, cancellationToken);
        if (applicationId is null)
            return NotFound();

        var access = await AuthorizeAsync(applicationId.Value, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        model.Id = id;
        model.ApplicationId = applicationId.Value;
        if (!ModelState.IsValid)
            return this.ModalInvalid(NoteFormPartial, model);

        var result = await _noteService.EditAsync(id, model.Text, cancellationToken);
        return this.ToModalResult(result, NoteFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var applicationId = await _notes.ApplicationIdAsync(id, cancellationToken);
        if (applicationId is null)
            return NotFound();

        var access = await AuthorizeAsync(applicationId.Value, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        return PartialView(ConfirmPartial, DeleteConfirmation(id));
    }

    [HttpPost]
    [ActionName(nameof(Delete))]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var applicationId = await _notes.ApplicationIdAsync(id, cancellationToken);
        if (applicationId is null)
            return NotFound();

        var access = await AuthorizeAsync(applicationId.Value, cancellationToken);
        if (access.Application is null)
            return this.DeniedResult(access);

        var result = await _noteService.RemoveAsync(id, cancellationToken);
        return this.ToModalResult(result, ConfirmPartial, DeleteConfirmation(id));
    }

    private Task<ApplicationAccess> AuthorizeAsync(int applicationId, CancellationToken cancellationToken)
    {
        return _pageBuilder.AuthorizeAsync(User, applicationId, ApplicationOperations.ManageNotes, cancellationToken);
    }

    private DeleteConfirmViewModel DeleteConfirmation(int noteId)
    {
        return new DeleteConfirmViewModel
        {
            Title = "Remove note",
            Message = "Remove this note? This can't be undone.",
            PostUrl = Url.Action(nameof(Delete), new { id = noteId })!
        };
    }
}
