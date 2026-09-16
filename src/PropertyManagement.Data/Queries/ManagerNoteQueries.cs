using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Entities;

namespace PropertyManagement.Data.Queries;

public sealed class ManagerNoteQueries
{
    private readonly PropertyManagementDbContext _db;

    public ManagerNoteQueries(PropertyManagementDbContext db)
    {
        _db = db;
    }

    public Task<ManagerNote?> FindAsync(int noteId, CancellationToken cancellationToken = default)
    {
        return _db.ManagerNotes
            .AsNoTracking()
            .SingleOrDefaultAsync(n => n.Id == noteId, cancellationToken);
    }

    public Task<int?> ApplicationIdAsync(int noteId, CancellationToken cancellationToken = default)
    {
        return _db.ManagerNotes
            .Where(n => n.Id == noteId)
            .Select(n => (int?)n.RentalApplicationId)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
