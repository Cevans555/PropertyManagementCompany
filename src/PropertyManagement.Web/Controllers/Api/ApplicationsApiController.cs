using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Core.Enums;
using PropertyManagement.Web.Models.Api;
using PropertyManagement.Web.Models.Grid;
using PropertyManagement.Web.Services;

namespace PropertyManagement.Web.Controllers.Api;

/// <summary>JSON endpoints behind the application grid.</summary>
[ApiController]
[Authorize]
[Route("api/applications")]
[Tags("Applications")]
public class ApplicationsApiController : ControllerBase
{
    private readonly ApplicationListQuery _query;

    public ApplicationsApiController(ApplicationListQuery query)
    {
        _query = query;
    }

    /// <summary>Lists applications, one page at a time.</summary>
    /// <remarks>
    /// Applicants get only the applications they're on; property managers get all of them. Filtering, sorting and
    /// paging run in the database, and <c>totalCount</c> is the number of applications matching the filters.
    /// </remarks>
    /// <response code="200">The requested page and the filtered total.</response>
    /// <response code="400">A parameter is invalid, such as an unknown sort column or a page size over 100.</response>
    /// <response code="401">Not signed in.</response>
    [HttpGet]
    [EndpointName("ListApplications")]
    [ProducesResponseType<PagedResult<ApplicationListItem>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<ApplicationListItem>>> List(
        [FromQuery] ApplicationListRequest request, CancellationToken cancellationToken)
    {
        var result = await _query.ListAsync(
            User,
            new ApplicationListFilter(request.Status, request.PropertyId),
            new ApplicationListPaging(request.Page, request.PageSize, request.Sort, request.Direction),
            cancellationToken);

        var items = result.Items
            .Select(row => new ApplicationListItem
            {
                Id = row.Id,
                PropertyName = row.PropertyName,
                UnitNumber = row.UnitNumber,
                Status = row.Status,
                StatusName = row.Status.DisplayName(),
                Applicants = row.Applicants,
                CreatedAt = row.CreatedAt,
                SubmittedAt = row.SubmittedAt,
                ClaimedBy = row.ClaimedBy,
                DetailsUrl = Url.Action("Details", "Applications", new { id = row.Id })!
            })
            .ToList();

        return new PagedResult<ApplicationListItem>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
