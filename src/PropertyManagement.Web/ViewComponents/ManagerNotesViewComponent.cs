using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Web.Models.Reviews;

namespace PropertyManagement.Web.ViewComponents;

public class ManagerNotesViewComponent : ViewComponent
{
    private readonly PropertyManagementDbContext _db;

    public ManagerNotesViewComponent(PropertyManagementDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync(int applicationId)
    {
        var notes = await _db.ManagerNotes
            .AsNoTracking()
            .Where(n => n.RentalApplicationId == applicationId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new ManagerNoteRowViewModel(
                n.Id,
                n.Text,
                _db.Users.Where(u => u.Id == n.CreatedById).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault() ?? n.CreatedById,
                n.CreatedAt,
                n.ModifiedAt,
                _db.Users.Where(u => u.Id == n.ModifiedById).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault()))
            .ToListAsync(HttpContext.RequestAborted);

        return View(new ManagerNotesViewModel(applicationId, notes));
    }
}
