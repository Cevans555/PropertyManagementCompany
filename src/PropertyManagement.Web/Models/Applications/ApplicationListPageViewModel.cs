using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.Models.Applications;

public sealed record ApplicationListPageViewModel(
    ApplicationStatus? Status,
    int? PropertyId,
    bool IsManager,
    IReadOnlyList<SelectListItem> StatusOptions,
    IReadOnlyList<SelectListItem> PropertyOptions);
