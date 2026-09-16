using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManagement.Core.Enums;
using PropertyManagement.Web.Infrastructure;
using PropertyManagement.Web.Models.Grid;

namespace PropertyManagement.Web.Models.Applications;

public sealed record ApplicationListPageViewModel
{
    public const string FilterFormId = "application-filters";

    public ApplicationStatus? Status { get; init; }

    public int? PropertyId { get; init; }

    public required bool IsManager { get; init; }

    public required IReadOnlyList<SelectListItem> StatusOptions { get; init; }

    public required IReadOnlyList<SelectListItem> PropertyOptions { get; init; }

    public GridViewModel Grid(string dataUrl)
    {
        var columns = new List<GridColumn>
        {
            new()
            {
                Key = "propertyName",
                Title = "Unit",
                SortKey = "property",
                SecondaryKey = "unitNumber",
                SecondaryPrefix = "Unit "
            },
            new()
            {
                Key = "applicants",
                Title = "Applicants"
            },
            new()
            {
                Key = "status",
                Title = "Status",
                SortKey = "status",
                Format = GridCellFormat.Badge,
                SecondaryKey = "statusName",
                BadgeClasses = StatusBadges.ByName
            },
            new()
            {
                Key = "submittedAt",
                Title = "Submitted",
                SortKey = "submitted",
                Format = GridCellFormat.Date,
                EmptyText = "Not submitted"
            }
        };

        if (IsManager)
        {
            columns.Add(new GridColumn
            {
                Key = "claimedBy",
                Title = "Claimed by",
                SortKey = "claimedBy",
                EmptyText = "-"
            });
        }

        columns.Add(new GridColumn
        {
            Key = "detailsUrl",
            Title = "Actions",
            Format = GridCellFormat.Link,
            LinkText = "Open",
            HideTitle = true
        });

        return new GridViewModel
        {
            Id = "application-grid",
            DataUrl = dataUrl,
            Caption = "Applications",
            Columns = columns,
            DefaultSort = "submitted",
            DefaultDirection = SortDirection.Desc,
            EmptyText = "No applications match these filters.",
            FilterFormId = FilterFormId
        };
    }
}
