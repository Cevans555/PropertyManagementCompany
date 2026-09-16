using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace PropertyManagement.Web.Authorization;

/// <summary>
/// Things a user can do to one specific rental application. Checked with
/// IAuthorizationService.AuthorizeAsync(User, application, ApplicationOperations.X).
/// </summary>
public static class ApplicationOperations
{
    /// <summary>Open the application page.</summary>
    public static readonly OperationAuthorizationRequirement View = new() { Name = nameof(View) };

    /// <summary>Change sections, residences, or submit. Also decides whether sections render editable.</summary>
    public static readonly OperationAuthorizationRequirement Edit = new() { Name = nameof(Edit) };

    /// <summary>Withdraw the application.</summary>
    public static readonly OperationAuthorizationRequirement Withdraw = new() { Name = nameof(Withdraw) };

    /// <summary>Claim, release, approve, return or deny.</summary>
    public static readonly OperationAuthorizationRequirement Review = new() { Name = nameof(Review) };

    /// <summary>See and edit internal manager notes. Never granted to applicants.</summary>
    public static readonly OperationAuthorizationRequirement ManageNotes = new() { Name = nameof(ManageNotes) };
}
