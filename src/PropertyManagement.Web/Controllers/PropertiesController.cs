using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Data;
using PropertyManagement.Data.Services;
using PropertyManagement.Web.Authorization;
using PropertyManagement.Web.Infrastructure;
using PropertyManagement.Web.Models;
using PropertyManagement.Web.Models.Properties;

namespace PropertyManagement.Web.Controllers;

[Authorize(Policy = Policies.PropertyManager)]
public class PropertiesController : Controller
{
    private const string PropertyFormPartial = "_PropertyForm";
    private const string DeleteConfirmPartial = "_DeleteConfirm";

    private readonly PropertyManagementDbContext _db;
    private readonly PropertyService _propertyService;

    public PropertiesController(PropertyManagementDbContext db, PropertyService propertyService)
    {
        _db = db;
        _propertyService = propertyService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult List()
    {
        return ViewComponent("PropertyList");
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var property = await _db.Properties
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PropertyHeaderViewModel(p.Id, p.Name, p.Address.Street, p.Address.City, p.Address.State, p.Address.PostalCode))
            .SingleOrDefaultAsync(cancellationToken);

        if (property is null)
            return NotFound();

        return View(property);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return PartialView(PropertyFormPartial, new PropertyFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(PropertyFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return this.ModalInvalid(PropertyFormPartial, model);

        var result = await _propertyService.CreatePropertyAsync(model.ToDetails(), cancellationToken);
        return ToModalResult(result, PropertyFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _db.Properties
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PropertyFormViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Street = p.Address.Street,
                City = p.Address.City,
                State = p.Address.State,
                PostalCode = p.Address.PostalCode
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (model is null)
            return NotFound();

        return PartialView(PropertyFormPartial, model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, PropertyFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (!ModelState.IsValid)
            return this.ModalInvalid(PropertyFormPartial, model);

        var result = await _propertyService.UpdatePropertyAsync(id, model.ToDetails(), cancellationToken);
        return ToModalResult(result, PropertyFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var model = await PropertyDeleteModelAsync(id, cancellationToken);
        if (model is null)
            return NotFound();

        return PartialView(DeleteConfirmPartial, model);
    }

    [HttpPost]
    [ActionName(nameof(Delete))]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await _propertyService.DeletePropertyAsync(id, cancellationToken);
        if (result.Succeeded)
            return this.ModalSuccess();

        var model = await PropertyDeleteModelAsync(id, cancellationToken);
        if (model is null)
            return NotFound();

        return ToModalResult(result, DeleteConfirmPartial, model);
    }

    private IActionResult ToModalResult(ServiceResult result, string partialViewName, object model)
    {
        if (result.Succeeded)
            return this.ModalSuccess();

        ModelState.AddModelError(string.Empty, result.Error!);
        return this.ModalInvalid(partialViewName, model);
    }

    private async Task<DeleteConfirmViewModel?> PropertyDeleteModelAsync(int id, CancellationToken cancellationToken)
    {
        var property = await _db.Properties
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { p.Name, UnitCount = p.Units.Count() })
            .SingleOrDefaultAsync(cancellationToken);

        if (property is null)
            return null;

        return new DeleteConfirmViewModel(
            "Remove property",
            $"Remove {property.Name} and its {property.UnitCount} unit(s)? This can't be undone.",
            Url.Action(nameof(Delete), new { id })!);
    }
}
