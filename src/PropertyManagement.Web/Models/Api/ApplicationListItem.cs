using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.Models.Api;

/// <summary>One application in the list.</summary>
/// <param name="Id">Application id.</param>
/// <param name="PropertyName">Property the unit belongs to.</param>
/// <param name="UnitNumber">Unit applied for.</param>
/// <param name="Status">Current status.</param>
/// <param name="StatusName">Status as shown to users, such as "Under Review".</param>
/// <param name="Applicants">Applicant names, primary applicant first.</param>
/// <param name="CreatedAt">When the application was started (UTC).</param>
/// <param name="SubmittedAt">When it was last submitted (UTC); null if never submitted.</param>
/// <param name="ClaimedBy">Manager reviewing it. Always null for applicants.</param>
/// <param name="DetailsUrl">The application page.</param>
public sealed record ApplicationListItem(
    int Id,
    string PropertyName,
    string UnitNumber,
    ApplicationStatus Status,
    string StatusName,
    string Applicants,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    string? ClaimedBy,
    string DetailsUrl);
