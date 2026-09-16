using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace PropertyManagement.Web.Authorization;

public static class ApplicationOperations
{
    public static readonly OperationAuthorizationRequirement View = new() { Name = nameof(View) };

    public static readonly OperationAuthorizationRequirement Edit = new() { Name = nameof(Edit) };

    public static readonly OperationAuthorizationRequirement Withdraw = new() { Name = nameof(Withdraw) };

    public static readonly OperationAuthorizationRequirement Review = new() { Name = nameof(Review) };

    public static readonly OperationAuthorizationRequirement ManageNotes = new() { Name = nameof(ManageNotes) };
}
