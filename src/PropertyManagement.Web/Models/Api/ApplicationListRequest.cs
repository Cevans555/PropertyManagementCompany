using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Core.Enums;
using PropertyManagement.Web.Models.Grid;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.Models.Api;

/// <summary>Query parameters for <c>GET /api/applications</c>.</summary>
public sealed class ApplicationListRequest
{
    public const int MaxPageSize = 100;

    /// <summary>Only applications in this status.</summary>
    [FromQuery(Name = "status")]
    public ApplicationStatus? Status { get; init; }

    /// <summary>Only applications for units in this property.</summary>
    [FromQuery(Name = "propertyId")]
    public int? PropertyId { get; init; }

    /// <summary>1-based page number. A page past the end returns the last page.</summary>
    [Range(1, int.MaxValue)]
    [FromQuery(Name = "page")]
    public int Page { get; init; } = 1;

    /// <summary>Rows per page, from 1 to 100.</summary>
    [Range(1, MaxPageSize)]
    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; } = 20;

    /// <summary>The column to sort by.</summary>
    [FromQuery(Name = "sort")]
    public ApplicationSortField Sort { get; init; } = ApplicationSortField.Submitted;

    /// <summary>Sort direction.</summary>
    [FromQuery(Name = "direction")]
    public SortDirection Direction { get; init; } = SortDirection.Desc;
}
