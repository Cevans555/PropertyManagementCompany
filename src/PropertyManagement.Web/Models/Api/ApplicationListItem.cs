using PropertyManagement.Core.Enums;

namespace PropertyManagement.Web.Models.Api;

/// <summary>One application in the list.</summary>
public sealed record ApplicationListItem
{
    /// <summary>Application id.</summary>
    public required int Id { get; init; }

    /// <summary>Property the unit belongs to.</summary>
    public required string PropertyName { get; init; }

    /// <summary>Unit applied for.</summary>
    public required string UnitNumber { get; init; }

    /// <summary>Current status.</summary>
    public required ApplicationStatus Status { get; init; }

    /// <summary>Status as shown to users, such as "Under Review".</summary>
    public required string StatusName { get; init; }

    /// <summary>Applicant names, primary applicant first.</summary>
    public required string Applicants { get; init; }

    /// <summary>When the application was started (UTC).</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>When it was last submitted (UTC); null if never submitted.</summary>
    public DateTime? SubmittedAt { get; init; }

    /// <summary>Manager reviewing it. Always null for applicants.</summary>
    public string? ClaimedBy { get; init; }

    /// <summary>The application page.</summary>
    public required string DetailsUrl { get; init; }
}
