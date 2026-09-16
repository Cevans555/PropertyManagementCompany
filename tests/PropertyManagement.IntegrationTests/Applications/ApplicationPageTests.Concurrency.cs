using System.Net;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Enums;
using PropertyManagement.IntegrationTests.Infrastructure;

namespace PropertyManagement.IntegrationTests;

// Bonus 5: two applicants editing one application at the same time. Saves to different sections must both
// succeed, and a second save to the same section is refused as stale instead of overwriting silently.
public partial class ApplicationPageTests
{
    [Fact]
    public async Task ApplicantDetails_SavedFromStalePage_IsRejected()
    {
        var (client, applicationId) = await StartApplicationAsync();
        var stalePage = await client.GetStringAsync($"/Applications/Details/{applicationId}");
        var staleRowVersion = TestHelpers.HiddenValue(stalePage, "ApplicantRowVersion");

        Assert.Equal(HttpStatusCode.Redirect, (await ContinueApplicantAsync(client, applicationId)).StatusCode);
        var response = await client.PostFormWithTokenAsync(
            stalePage, $"/Applications/Details/{applicationId}", ApplicantFields(staleRowVersion, "Stale"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("changed by someone else", await response.Content.ReadAsStringAsync());
        var firstName = await _factory.QueryDbAsync(db =>
            db.Applicants.Where(a => a.RentalApplicationId == applicationId).Select(a => a.FirstName).SingleAsync());
        Assert.Equal("Jane", firstName);
    }

    [Fact]
    public async Task ResidenceSection_SavedFromStalePage_IsRejected()
    {
        var (client, applicationId) = await StartApplicationAsync();
        var stalePage = await client.GetStringAsync($"/Applications/Details/{applicationId}?section=Residences");
        var staleVersion = TestHelpers.HiddenValue(stalePage, "ResidenceSectionVersion");

        Assert.Equal(HttpStatusCode.OK, (await AddResidenceAsync(client, applicationId)).StatusCode);

        var response = await client.PostFormWithTokenAsync(stalePage, $"/Applications/Details/{applicationId}", new()
        {
            ["Section"] = "Residences",
            ["command"] = ApplicationCommandValues.Continue,
            ["ResidenceSectionVersion"] = staleVersion
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("changed by someone else", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ConcurrentSavesToDifferentSections_BothSucceed()
    {
        var (client, applicationId) = await StartApplicationAsync();
        var addUrl = $"/ApplicationApplicants/Add?applicationId={applicationId}";
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostFormAsync(addUrl, addUrl, new() { ["Email"] = TestAccounts.OtherApplicant })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AddResidenceAsync(client, applicationId)).StatusCode);

        var coApplicant = await _factory.CreateSignedInClientAsync(TestAccounts.OtherApplicant);

        var coApplicantPage = await coApplicant.GetStringAsync($"/Applications/Details/{applicationId}?section=Applicant");
        var coRowVersion = TestHelpers.HiddenValue(coApplicantPage, "ApplicantRowVersion");
        var residencePage = await client.GetStringAsync($"/Applications/Details/{applicationId}?section=Residences");
        var residenceVersion = TestHelpers.HiddenValue(residencePage, "ResidenceSectionVersion");

        var coApplicantSave = await coApplicant.PostFormWithTokenAsync(
            coApplicantPage, $"/Applications/Details/{applicationId}", ApplicantFields(coRowVersion, "Alex"));

        var residenceSave = await client.PostFormWithTokenAsync(residencePage, $"/Applications/Details/{applicationId}", new()
        {
            ["Section"] = "Residences",
            ["command"] = ApplicationCommandValues.Continue,
            ["ResidenceSectionVersion"] = residenceVersion
        });

        Assert.Equal(HttpStatusCode.Redirect, coApplicantSave.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, residenceSave.StatusCode);

        var coApplicantFirstName = await _factory.QueryDbAsync(db => db.Applicants
            .Where(a => a.RentalApplicationId == applicationId && !a.IsPrimary)
            .Select(a => a.FirstName)
            .SingleAsync());
        var residenceSectionSavedAt = await _factory.QueryDbAsync(db => db.RentalApplications
            .Where(a => a.Id == applicationId)
            .Select(a => a.ResidenceSectionSavedAt)
            .SingleAsync());

        Assert.Equal("Alex", coApplicantFirstName);
        Assert.NotNull(residenceSectionSavedAt);
    }
}
