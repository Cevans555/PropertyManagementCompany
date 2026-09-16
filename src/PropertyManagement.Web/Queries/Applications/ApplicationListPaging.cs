using PropertyManagement.Web.Models.Grid;

namespace PropertyManagement.Web.Queries.Applications;

public sealed record ApplicationListPaging(int Page, int PageSize, ApplicationSortField Sort, SortDirection Direction);
