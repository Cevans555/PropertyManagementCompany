using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Data.Services;

public sealed class ManagerNoteService
{
    public const string ApplicationNotFoundMessage = "Application not found.";
    public const string NoteNotFoundMessage = "Note not found.";

    private readonly PropertyManagementDbContext _db;

    public ManagerNoteService(PropertyManagementDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult> AddAsync(int applicationId, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var applicationExists = await _db.RentalApplications.AnyAsync(a => a.Id == applicationId, cancellationToken);
            if (!applicationExists)
                return ServiceResult.Failure(ApplicationNotFoundMessage);

            _db.ManagerNotes.Add(new ManagerNote(applicationId, text));
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> EditAsync(int noteId, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var note = await _db.ManagerNotes.SingleOrDefaultAsync(n => n.Id == noteId, cancellationToken);
            if (note is null)
                return ServiceResult.Failure(NoteNotFoundMessage);

            note.Edit(text);
            await _db.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return Fail(ex.Message);
        }
    }

    public async Task<ServiceResult> RemoveAsync(int noteId, CancellationToken cancellationToken = default)
    {
        var note = await _db.ManagerNotes.SingleOrDefaultAsync(n => n.Id == noteId, cancellationToken);
        if (note is null)
            return ServiceResult.Failure(NoteNotFoundMessage);

        _db.ManagerNotes.Remove(note);
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private ServiceResult Fail(string error)
    {
        _db.ChangeTracker.Clear();
        return ServiceResult.Failure(error);
    }
}
