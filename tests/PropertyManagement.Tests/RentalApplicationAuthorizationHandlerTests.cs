using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using PropertyManagement.Core.Entities;
using PropertyManagement.Core.Security;
using PropertyManagement.Web.Authorization;
using static PropertyManagement.Tests.TestSupport.TestData;

namespace PropertyManagement.Tests;

public class RentalApplicationAuthorizationHandlerTests
{
    private static readonly ClaimsPrincipal Applicant = User(ApplicantId, Roles.Applicant);
    private static readonly ClaimsPrincipal OtherApplicant = User(OtherApplicantId, Roles.Applicant);
    private static readonly ClaimsPrincipal Manager = User(ManagerId, Roles.PropertyManager);

    [Fact]
    public async Task ApplicantOnApplication_CanViewEditAndWithdrawDraft()
    {
        var draft = Draft();

        Assert.True(await IsAllowed(Applicant, draft, ApplicationOperations.View));
        Assert.True(await IsAllowed(Applicant, draft, ApplicationOperations.Edit));
        Assert.True(await IsAllowed(Applicant, draft, ApplicationOperations.Withdraw));
    }

    [Fact]
    public async Task ApplicantOnApplication_CannotEditOnceSubmitted()
    {
        var submitted = Submitted();

        Assert.True(await IsAllowed(Applicant, submitted, ApplicationOperations.View));
        Assert.False(await IsAllowed(Applicant, submitted, ApplicationOperations.Edit));
    }

    [Fact]
    public async Task Applicant_CannotReviewOrSeeNotes()
    {
        var submitted = Submitted();

        Assert.False(await IsAllowed(Applicant, submitted, ApplicationOperations.Review));
        Assert.False(await IsAllowed(Applicant, submitted, ApplicationOperations.ManageNotes));
    }

    [Fact]
    public async Task ApplicantNotOnApplication_IsDeniedEverything()
    {
        var draft = Draft();

        Assert.False(await IsAllowed(OtherApplicant, draft, ApplicationOperations.View));
        Assert.False(await IsAllowed(OtherApplicant, draft, ApplicationOperations.Edit));
        Assert.False(await IsAllowed(OtherApplicant, draft, ApplicationOperations.Withdraw));
    }

    [Fact]
    public async Task CoApplicant_HasSameAccessAsPrimary()
    {
        var draft = Draft();
        draft.AddApplicant(ApplicantId, OtherApplicantId);

        Assert.True(await IsAllowed(OtherApplicant, draft, ApplicationOperations.View));
        Assert.True(await IsAllowed(OtherApplicant, draft, ApplicationOperations.Edit));
    }

    [Fact]
    public async Task Manager_CanViewReviewAndManageNotes_ButNotEditOrWithdraw()
    {
        var submitted = Submitted();

        Assert.True(await IsAllowed(Manager, submitted, ApplicationOperations.View));
        Assert.True(await IsAllowed(Manager, submitted, ApplicationOperations.Review));
        Assert.True(await IsAllowed(Manager, submitted, ApplicationOperations.ManageNotes));
        Assert.False(await IsAllowed(Manager, submitted, ApplicationOperations.Edit));
        Assert.False(await IsAllowed(Manager, submitted, ApplicationOperations.Withdraw));
    }

    [Fact]
    public async Task UserOnApplicationWithoutApplicantRole_IsDenied()
    {
        var draft = Draft();
        var noRole = User(ApplicantId);

        Assert.False(await IsAllowed(noRole, draft, ApplicationOperations.View));
    }

    [Fact]
    public async Task Withdraw_IsDenied_ForTerminalApplication()
    {
        var application = Submitted();
        application.Withdraw(ApplicantId, Now);

        Assert.False(await IsAllowed(Applicant, application, ApplicationOperations.Withdraw));
    }

    private static async Task<bool> IsAllowed(
        ClaimsPrincipal user, RentalApplication application, OperationAuthorizationRequirement operation)
    {
        var context = new AuthorizationHandlerContext([operation], user, application);
        await new RentalApplicationAuthorizationHandler().HandleAsync(context);
        return context.HasSucceeded;
    }

    private static ClaimsPrincipal User(string userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }
}
