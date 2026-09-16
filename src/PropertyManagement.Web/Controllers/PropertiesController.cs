using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Data.Services;
using PropertyManagement.Web.Authorization;
using PropertyManagement.Web.Infrastructure;
using PropertyManagement.Web.Models;
using PropertyManagement.Web.Models.Properties;
using PropertyManagement.Web.Queries;

namespace PropertyManagement.Web.Controllers;

[Authorize(Policy = Policies.PropertyManager)]
public class PropertiesController : Controller
{
    private const string PropertyFormPartial = "_PropertyForm";
    private const string DeleteConfirmPartial = "_DeleteConfirm";

    private readonly PropertyQueries _properties;
    private readonly PropertyService _propertyService;

    public PropertiesController(PropertyQueries properties, PropertyService propertyService)
    {
        _properties = properties;
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
        var property = await _properties.HeaderAsync(id, cancellationToken);

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
        return this.ToModalResult(result, PropertyFormPartial, model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _properties.FormAsync(id, cancellationToken);

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
        return this.ToModalResult(result, PropertyFormPartial, model);
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

        return this.ToModalResult(result, DeleteConfirmPartial, model);
    }


    private async Task<DeleteConfirmViewModel?> PropertyDeleteModelAsync(int id, CancellationToken cancellationToken)
    {
        var property = await _properties.DeleteInfoAsync(id, cancellationToken);

        if (property is null)
            return null;

        return new DeleteConfirmViewModel
        {
            Title = "Remove property",
            Message = $"Remove {property.Name} and its {property.UnitCount} unit(s)? This can't be undone.",
            PostUrl = Url.Action(nameof(Delete), new { id })!
        };
    }
}
