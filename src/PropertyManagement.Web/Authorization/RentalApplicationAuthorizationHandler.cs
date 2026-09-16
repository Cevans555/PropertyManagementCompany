using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Enums;
using PropertyManagement.Core.Security;

namespace PropertyManagement.Web.Authorization;

public sealed class RentalApplicationAuthorizationHandler
    : AuthorizationHandler<OperationAuthorizationRequirement, RentalApplication>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        RentalApplication application)
    {
        if (IsAllowed(context.User, requirement, application))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }

    private static bool IsAllowed(
        ClaimsPrincipal user, OperationAuthorizationRequirement requirement, RentalApplication application)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var isManager = user.IsInRole(Roles.PropertyManager);
        var isApplicantOnApplication = userId is not null
            && user.IsInRole(Roles.Applicant)
            && application.IsApplicant(userId);

        switch (requirement.Name)
        {
            case nameof(ApplicationOperations.View):
                return isManager || isApplicantOnApplication;
            case nameof(ApplicationOperations.Edit):
                return isApplicantOnApplication && application.IsEditable;
            case nameof(ApplicationOperations.Withdraw):
                return isApplicantOnApplication && !application.Status.IsTerminal();
            case nameof(ApplicationOperations.Review):
                return isManager;
            case nameof(ApplicationOperations.ManageNotes):
                return isManager;
            default:
                return false;
        }
    }
}
