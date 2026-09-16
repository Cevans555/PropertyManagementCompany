using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Data.Services;
using PropertyManagement.Web.Authorization;
using PropertyManagement.Web.Infrastructure;
using PropertyManagement.Web.Models;
using PropertyManagement.Web.Models.Units;

namespace PropertyManagement.Web.Controllers;

[Authorize(Policy = Policies.PropertyManager)]
public class UnitsController : Controller
{
    private const string UnitFormPartial = "_UnitForm";
    private const string DeleteConfirmPartial = "_DeleteConfirm";

    private readonly PropertyManagementDbContext _db;
    private readonly PropertyService _propertyService;

    public UnitsController(PropertyManagementDbContext db, PropertyService propertyService)
    {
        _db = db;
        _propertyService = propertyService;
    }

    [HttpGet]
    public IActionResult Table(int id)
    {
        return ViewComponent("UnitTable", new { propertyId = id });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int propertyId, CancellationToken cancellationToken)
    {
        var propertyName = await PropertyNameAsync(propertyId, cancellationToken);
        if (propertyName is null)
            return NotFound();

        var model = new UnitFormViewModel
        {
            PropertyId = propertyId,
            PropertyName = propertyName,
            UnitTypeOptions = await UnitTypeOptionsAsync(currentUnitTypeId: null, cancellationToken)
        };

        return PartialView(UnitFormPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int propertyId, UnitFormViewModel model, CancellationToken cancellationToken)
    {
        var propertyName = await PropertyNameAsync(propertyId, cancellationToken);
        if (propertyName is null)
            return NotFound();

        if (ModelState.IsValid)
        {
            var result = await _propertyService.AddUnitAsync(propertyId, model.ToDetails(), cancellationToken);
            if (result.Succeeded)
                return this.ModalSuccess();

            ModelState.AddModelError(string.Empty, result.Error!);
        }

        model.PropertyId = propertyId;
        model.PropertyName = propertyName;
        model.UnitTypeOptions = await UnitTypeOptionsAsync(currentUnitTypeId: null, cancellationToken);
        return this.ModalInvalid(UnitFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _db.Units
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UnitFormViewModel
            {
                Id = u.Id,
                PropertyId = u.PropertyId,
                PropertyName = u.Property.Name,
                UnitNumber = u.UnitNumber,
                Bedrooms = u.Bedrooms,
                MonthlyRent = u.MonthlyRent,
                UnitTypeId = u.UnitTypeId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (model is null)
            return NotFound();

        model.UnitTypeOptions = await UnitTypeOptionsAsync(model.UnitTypeId, cancellationToken);
        return PartialView(UnitFormPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, UnitFormViewModel model, CancellationToken cancellationToken)
    {
        var current = await _db.Units
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.PropertyId, PropertyName = u.Property.Name, u.UnitTypeId })
            .SingleOrDefaultAsync(cancellationToken);

        if (current is null)
            return NotFound();

        if (ModelState.IsValid)
        {
            var result = await _propertyService.UpdateUnitAsync(id, model.ToDetails(), cancellationToken);
            if (result.Succeeded)
                return this.ModalSuccess();

            ModelState.AddModelError(string.Empty, result.Error!);
        }

        model.Id = id;
        model.PropertyId = current.PropertyId;
        model.PropertyName = current.PropertyName;
        model.UnitTypeOptions = await UnitTypeOptionsAsync(current.UnitTypeId, cancellationToken);
        return this.ModalInvalid(UnitFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await UnitDeleteModelAsync(id, cancellationToken);
        if (model is null)
            return NotFound();

        return PartialView(DeleteConfirmPartial, model);
    }

    [HttpPost]
    [ActionName(nameof(Delete))]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await _propertyService.DeleteUnitAsync(id, cancellationToken);
        if (result.Succeeded)
            return this.ModalSuccess();

        var model = await UnitDeleteModelAsync(id, cancellationToken);
        if (model is null)
            return NotFound();

        ModelState.AddModelError(string.Empty, result.Error!);
        return this.ModalInvalid(DeleteConfirmPartial, model);
    }

    private Task<string?> PropertyNameAsync(int propertyId, CancellationToken cancellationToken)
    {
        return _db.Properties
            .Where(p => p.Id == propertyId)
            .Select(p => p.Name)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<SelectListItem>> UnitTypeOptionsAsync(int? currentUnitTypeId, CancellationToken cancellationToken)
    {
        var unitTypes = await _db.UnitTypes
            .AsNoTracking()
            .Where(t => t.IsActive || t.Id == currentUnitTypeId)
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name, t.IsActive })
            .ToListAsync(cancellationToken);

        return unitTypes
            .Select(t => new SelectListItem(
                t.IsActive ? t.Name : $"{t.Name} (inactive)",
                t.Id.ToString(CultureInfo.InvariantCulture)))
            .ToList();
    }

    private async Task<DeleteConfirmViewModel?> UnitDeleteModelAsync(int id, CancellationToken cancellationToken)
    {
        var unit = await _db.Units
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.UnitNumber, PropertyName = u.Property.Name })
            .SingleOrDefaultAsync(cancellationToken);

        if (unit is null)
            return null;

        return new DeleteConfirmViewModel
        {
            Title = "Remove unit",
            Message = $"Remove unit {unit.UnitNumber} from {unit.PropertyName}? This can't be undone.",
            PostUrl = Url.Action(nameof(Delete), new { id })!
        };
    }
}
