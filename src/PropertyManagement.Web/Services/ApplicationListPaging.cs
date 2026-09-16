using PropertyManagement.Web.Models.Grid;

namespace PropertyManagement.Web.Services;

public sealed record ApplicationListPaging(int Page, int PageSize, ApplicationSortField Sort, SortDirection Direction);
