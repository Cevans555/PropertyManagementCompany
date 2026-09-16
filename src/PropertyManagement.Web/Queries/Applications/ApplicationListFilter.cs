using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.Queries.Applications;

public sealed record ApplicationListFilter(ApplicationStatus? Status, int? PropertyId);
