using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Data.Services;
using PropertyManagement.Web.Authorization;
using PropertyManagement.Web.Infrastructure;
using PropertyManagement.Web.Models;
using PropertyManagement.Web.Models.Units;
using PropertyManagement.Web.Queries;

namespace PropertyManagement.Web.Controllers;

[Authorize(Policy = Policies.PropertyManager)]
public class UnitsController : Controller
{
    private const string UnitFormPartial = "_UnitForm";
    private const string DeleteConfirmPartial = "_DeleteConfirm";

    private readonly UnitQueries _units;
    private readonly PropertyQueries _properties;
    private readonly PropertyService _propertyService;

    public UnitsController(UnitQueries units, PropertyQueries properties, PropertyService propertyService)
    {
        _units = units;
        _properties = properties;
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
        var propertyName = await _properties.NameAsync(propertyId, cancellationToken);
        if (propertyName is null)
            return NotFound();

        var model = new UnitFormViewModel
        {
            PropertyId = propertyId,
            PropertyName = propertyName,
            UnitTypeOptions = await _units.TypeOptionsAsync(currentUnitTypeId: null, cancellationToken)
        };

        return PartialView(UnitFormPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int propertyId, UnitFormViewModel model, CancellationToken cancellationToken)
    {
        var propertyName = await _properties.NameAsync(propertyId, cancellationToken);
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
        model.UnitTypeOptions = await _units.TypeOptionsAsync(currentUnitTypeId: null, cancellationToken);
        return this.ModalInvalid(UnitFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _units.FormAsync(id, cancellationToken);
        if (model is null)
            return NotFound();

        model.UnitTypeOptions = await _units.TypeOptionsAsync(model.UnitTypeId, cancellationToken);
        return PartialView(UnitFormPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, UnitFormViewModel model, CancellationToken cancellationToken)
    {
        var current = await _units.FormContextAsync(id, cancellationToken);
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
        model.UnitTypeOptions = await _units.TypeOptionsAsync(current.UnitTypeId, cancellationToken);
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

    private async Task<DeleteConfirmViewModel?> UnitDeleteModelAsync(int id, CancellationToken cancellationToken)
    {
        var unit = await _units.DeleteInfoAsync(id, cancellationToken);
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
