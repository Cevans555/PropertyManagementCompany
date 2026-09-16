using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManagement.Core.Enums;
using PropertyManagement.Web.Infrastructure;
using PropertyManagement.Web.Models.Grid;

namespace PropertyManagement.Web.Models.Applications;

public sealed record ApplicationListPageViewModel(
    ApplicationStatus? Status,
    int? PropertyId,
    bool IsManager,
    IReadOnlyList<SelectListItem> StatusOptions,
    IReadOnlyList<SelectListItem> PropertyOptions)
{
    public const string FilterFormId = "application-filters";

    public GridViewModel Grid(string dataUrl)
    {
        var columns = new List<GridColumn>
        {
            new("propertyName", "Unit")
            {
                SortKey = "property",
                SecondaryKey = "unitNumber",
                SecondaryPrefix = "Unit "
            },
            new("applicants", "Applicants"),
            new("status", "Status")
            {
                SortKey = "status",
                Format = GridCellFormat.Badge,
                SecondaryKey = "statusName",
                BadgeClasses = StatusBadges.ByName
            },
            new("submittedAt", "Submitted")
            {
                SortKey = "submitted",
                Format = GridCellFormat.Date,
                EmptyText = "Not submitted"
            }
        };

        if (IsManager)
        {
            columns.Add(new GridColumn("claimedBy", "Claimed by")
            {
                SortKey = "claimedBy",
                EmptyText = "-"
            });
        }

        columns.Add(new GridColumn("detailsUrl", "Actions")
        {
            Format = GridCellFormat.Link,
            LinkText = "Open",
            HideTitle = true
        });

        return new GridViewModel("application-grid", dataUrl, "Applications", columns)
        {
            DefaultSort = "submitted",
            DefaultDirection = SortDirection.Desc,
            EmptyText = "No applications match these filters.",
            FilterFormId = FilterFormId
        };
    }
}
