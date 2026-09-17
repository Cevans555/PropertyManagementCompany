using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using PropertyManagement.BrowserTests.Infrastructure;
using PropertyManagement.Core.Entities;
using PropertyManagement.IntegrationTests.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace PropertyManagement.BrowserTests;

[Collection(BrowserCollection.Name)]
public class ApplicationModalTests
{
    private readonly BrowserFixture _fixture;

    public ApplicationModalTests(BrowserFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ResidenceModal_ServerError_ThenClientValidationStillWiredUp()
    {
        var applicationId = await DraftWithResidenceAsync();
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var _ = context;
        var posts = 0;
        page.Request += (_, request) =>
        {
            if (request.Method == "POST")
                posts++;
        };

        await page.GotoAsync($"/Applications/Details/{applicationId}?section=Residences");
        var modal = page.Modal();
        await page.Locator("[data-modal-url*='/ApplicationResidences/Create']").ClickAsync();
        await FillResidenceAsync(modal, moveIn: "2025-01-01", moveOut: "2020-01-01");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Add residence" }).ClickAsync();

        await Expect(modal).ToContainTextAsync("Move-out date can't be before the move-in date.");
        Assert.Equal(1, posts);

        await modal.GetByLabel("Street", new() { Exact = true }).FillAsync("");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Add residence" }).ClickAsync();
        await Expect(modal.GetByText("The Street field is required.")).ToBeVisibleAsync();
        Assert.Equal(1, posts);
    }

    [Fact]
    public async Task EditAndRemoveResidence_RefreshTheListWithoutReload()
    {
        var applicationId = await DraftWithResidenceAsync();
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var _ = context;
        await page.GotoAsync($"/Applications/Details/{applicationId}?section=Residences");
        await page.MarkPageAsync();
        var modal = page.Modal();
        var list = page.Locator("#residence-list");

        await list.GetByRole(AriaRole.Button, new() { Name = "Edit" }).First.ClickAsync();
        await modal.GetByLabel("Landlord name").FillAsync("Renamed Landlord");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Save changes" }).ClickAsync();
        await Expect(modal).ToBeHiddenAsync();
        await Expect(list).ToContainTextAsync("Renamed Landlord");

        await list.GetByRole(AriaRole.Button, new() { Name = "Remove" }).First.ClickAsync();
        await Expect(modal.GetByRole(AriaRole.Heading, new() { Name = "Remove residence" })).ToBeVisibleAsync();
        await modal.GetByRole(AriaRole.Button, new() { Name = "Remove", Exact = true }).ClickAsync();
        await Expect(modal).ToBeHiddenAsync();
        await Expect(list).ToContainTextAsync("No residences added yet.");

        await page.AssertNotReloadedAsync();
    }

    [Fact]
    public async Task Back_ReturnsToThePreviousSection_WithoutSaving()
    {
        var applicationId = await DraftWithResidenceAsync();
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var _ = context;

        await page.GotoAsync($"/Applications/Details/{applicationId}");
        await page.GetByLabel("Phone").FillAsync("(518) 555-0142");
        await page.GetByLabel("Street", new() { Exact = true }).FillAsync("9 Unsaved Way");
        await page.GotoAsync($"/Applications/Details/{applicationId}?section=Residences");
        await page.GetByRole(AriaRole.Button, new() { Name = "Back" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Applicant information" })).ToBeVisibleAsync();
        await Expect(page.GetByLabel("Street", new() { Exact = true })).Not.ToHaveValueAsync("9 Unsaved Way");
        await Expect(page.Locator(".section-steps .is-current")).ToContainTextAsync("Applicant information");
    }

    [Fact]
    public async Task WithdrawModal_ReloadsPageWithWithdrawnStatus()
    {
        var applicationId = await DraftWithResidenceAsync();
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var _ = context;
        await page.GotoAsync($"/Applications/Details/{applicationId}");
        var modal = page.Modal();

        await page.GetByRole(AriaRole.Button, new() { Name = "Withdraw" }).ClickAsync();
        await modal.GetByRole(AriaRole.Button, new() { Name = "Withdraw application" }).ClickAsync();

        await Expect(page.Locator(".badge", new() { HasText = "Withdrawn" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Withdraw" })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task CoApplicantModal_UnknownEmailError_ThenAddsThem()
    {
        var applicationId = await DraftWithResidenceAsync();
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var _ = context;
        await page.GotoAsync($"/Applications/Details/{applicationId}");
        await page.MarkPageAsync();
        var modal = page.Modal();

        await page.Locator("[data-modal-url*='/ApplicationApplicants/Add']").ClickAsync();
        await modal.GetByLabel("Co-applicant's email").FillAsync("nobody@nowhere.test");
        await modal.GetByRole(AriaRole.Button, new() { Name = "Add co-applicant" }).ClickAsync();
        await Expect(modal).ToContainTextAsync("No applicant account uses that email address.");

        await modal.GetByLabel("Co-applicant's email").FillAsync(TestAccounts.OtherApplicant);
        await modal.GetByRole(AriaRole.Button, new() { Name = "Add co-applicant" }).ClickAsync();

        await Expect(modal).ToBeHiddenAsync();
        var otherName = await _fixture.App.QueryDbAsync(db =>
            db.Users.Where(u => u.Email == TestAccounts.OtherApplicant).Select(u => u.FirstName).SingleAsync());
        await Expect(page.Locator("#applicant-list")).ToContainTextAsync(otherName);
        await page.AssertNotReloadedAsync();
    }

    [Fact]
    public async Task AvailableUnits_OffersToContinueAnOpenApplication()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        var applicantId = await _fixture.App.UserIdAsync(TestAccounts.Applicant);
        var applicationId = await _fixture.App.CreateApplicationAsync(unitId, applicantId, submit: false);
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var _ = context;

        await page.GotoAsync("/Applications/Available");
        await page.Locator($"a[href$='/Applications/Details/{applicationId}']", new() { HasText = "Continue application" }).ClickAsync();

        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"/Applications/Details/{applicationId}"));
    }

    [Fact]
    public async Task SubmittedApplication_IsReadOnly_ButStillBrowsable()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        var applicationId = await _fixture.App.CreateApplicationAsync(unitId, await _fixture.App.UserIdAsync(TestAccounts.Applicant));
        var (context, page) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var _ = context;

        await page.GotoAsync($"/Applications/Details/{applicationId}");
        await Expect(page.GetByText("so it can't be changed right now")).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Continue" })).ToHaveCountAsync(0);

        await page.GetByRole(AriaRole.Link, new() { Name = "Applicant information" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Applicant information" })).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Textbox)).ToHaveCountAsync(0);
        await page.GetByRole(AriaRole.Link, new() { Name = "Next" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Residence history" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SameSectionSavedByTwoApplicants_SecondIsToldToReload()
    {
        var applicationId = await DraftWithResidenceAsync();
        var primaryId = await _fixture.App.UserIdAsync(TestAccounts.Applicant);
        var coApplicantId = await _fixture.App.UserIdAsync(TestAccounts.OtherApplicant);
        await _fixture.App.QueryDbAsync(async db =>
        {
            var application = await db.RentalApplications.Include(a => a.Applicants).SingleAsync(a => a.Id == applicationId);
            application.AddApplicant(primaryId, coApplicantId);
            return await db.SaveChangesAsync();
        });

        var (firstContext, first) = await _fixture.SignInAsync(TestAccounts.Applicant);
        await using var _ = firstContext;
        var (secondContext, second) = await _fixture.SignInAsync(TestAccounts.OtherApplicant);
        await using var __ = secondContext;
        var url = $"/Applications/Details/{applicationId}?section=Residences";
        await first.GotoAsync(url);
        await second.GotoAsync(url);

        await second.GetByRole(AriaRole.Button, new() { Name = "Continue" }).ClickAsync();
        await Expect(second).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("section=Summary"));

        await first.GetByRole(AriaRole.Button, new() { Name = "Continue" }).ClickAsync();
        await Expect(first.GetByText("This application was changed by someone else. Reload the page and try again.")).ToBeVisibleAsync();
    }

    private async Task<int> DraftWithResidenceAsync()
    {
        var unitId = await _fixture.App.CreateUnitAsync();
        var applicantId = await _fixture.App.UserIdAsync(TestAccounts.Applicant);

        return await _fixture.App.QueryDbAsync(async db =>
        {
            var application = RentalApplication.Start(unitId, applicantId, unitHasActiveLease: false, DateTime.UtcNow);
            application.AddResidence(applicantId, TestHelpers.Residence());
            db.RentalApplications.Add(application);
            await db.SaveChangesAsync();
            return application.Id;
        });
    }

    private static async Task FillResidenceAsync(ILocator modal, string moveIn, string moveOut)
    {
        await modal.GetByLabel("Street", new() { Exact = true }).FillAsync("22 Elm St");
        await modal.GetByLabel("City", new() { Exact = true }).FillAsync("Troy");
        await modal.GetByLabel("State", new() { Exact = true }).FillAsync("NY");
        await modal.GetByLabel("Postal code").FillAsync("12180");
        await modal.GetByLabel("Landlord name").FillAsync("Bob Landlord");
        await modal.GetByLabel("Landlord phone").FillAsync("(518) 555-0199");
        await modal.GetByLabel("Move-in date").FillAsync(moveIn);
        await modal.GetByLabel("Move-out date").FillAsync(moveOut);
    }
}
