using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.Services;

public sealed record ApplicationListFilter(ApplicationStatus? Status, int? PropertyId);
