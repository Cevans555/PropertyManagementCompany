using System.Net;
using PropertyManagement.Core.Enums;
using PropertyManagement.IntegrationTests.Infrastructure;

namespace PropertyManagement.IntegrationTests;

public partial class ApplicationPageTests
{
    [Fact]
    public async Task SaveInvalidSections_ContinueSavesApplicantDetailsWithErrors_ShowsThem_AndBlocksSubmit()
    {
        using var _ = _factory.EnableSaveInvalidSections();
        var (client, applicationId) = await StartApplicationAsync();

        var response = await ContinueApplicantAsync(client, applicationId, firstName: "");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("section=Residences", response.Headers.Location!.OriginalString);
        Assert.NotNull(await DetailsSavedAtAsync(applicationId));
        Assert.Contains("saved with errors", await client.GetStringAsync(response.Headers.Location));

        var section = await client.GetStringAsync($"/Applications/Details/{applicationId}?section=Applicant");
        Assert.Contains("First name is required.", section);
        Assert.Contains("input-validation-error", section);

        var summary = await client.GetStringAsync($"/Applications/Details/{applicationId}?section=Summary");
        Assert.Contains("Applicant information: First name is required.", summary);
        Assert.Contains("disabled=\"disabled\"", summary);

        var submitted = await PostCommandAsync(client, applicationId, "Summary", ApplicationCommandValues.Submit);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, submitted.StatusCode);
        Assert.Equal(ApplicationStatus.Draft, await StatusAsync(applicationId));
    }

    [Fact]
    public async Task SaveInvalidSections_ValueTooLongToStore_IsStillRejected()
    {
        using var _ = _factory.EnableSaveInvalidSections();
        var (client, applicationId) = await StartApplicationAsync();

        var response = await ContinueApplicantAsync(client, applicationId, firstName: new string('a', 101));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("First name must be 100 characters or fewer.", await response.Content.ReadAsStringAsync());
        Assert.Null(await DetailsSavedAtAsync(applicationId));
    }

    [Fact]
    public async Task SaveInvalidSections_ResidenceSectionWithoutResidences_IsSavedAndListedOnSummary()
    {
        using var _ = _factory.EnableSaveInvalidSections();
        var (client, applicationId) = await StartApplicationAsync();

        var response = await PostCommandAsync(client, applicationId, "Residences", ApplicationCommandValues.Continue);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("section=Summary", response.Headers.Location!.OriginalString);

        var summary = await client.GetStringAsync($"/Applications/Details/{applicationId}?section=Summary");
        Assert.Contains("Residence history: Add at least one prior residence.", summary);
        Assert.Contains(
            "Add at least one prior residence.",
            await client.GetStringAsync($"/Applications/Details/{applicationId}?section=Residences"));
    }

    [Fact]
    public async Task SaveInvalidSections_FixingEveryError_AllowsSubmit()
    {
        using var _ = _factory.EnableSaveInvalidSections();
        var (client, applicationId) = await StartApplicationAsync();

        Assert.Equal(HttpStatusCode.Redirect, (await ContinueApplicantAsync(client, applicationId, firstName: "")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await ContinueApplicantAsync(client, applicationId)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AddResidenceAsync(client, applicationId)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Redirect,
            (await PostCommandAsync(client, applicationId, "Residences", ApplicationCommandValues.Continue)).StatusCode);

        Assert.DoesNotContain("Before you can submit", await client.GetStringAsync($"/Applications/Details/{applicationId}?section=Summary"));

        var submitted = await PostCommandAsync(client, applicationId, "Summary", ApplicationCommandValues.Submit);

        Assert.Equal(HttpStatusCode.Redirect, submitted.StatusCode);
        Assert.Equal(ApplicationStatus.Submitted, await StatusAsync(applicationId));
    }

    [Fact]
    public async Task SectionSavedWithErrors_StillBlocksSubmit_AfterSwitchIsTurnedOff()
    {
        HttpClient client;
        int applicationId;

        using (_factory.EnableSaveInvalidSections())
        {
            (client, applicationId) = await StartApplicationAsync();
            await ContinueApplicantAsync(client, applicationId, firstName: "");
        }

        var submitted = await PostCommandAsync(client, applicationId, "Summary", ApplicationCommandValues.Submit);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, submitted.StatusCode);
        Assert.Equal(ApplicationStatus.Draft, await StatusAsync(applicationId));
    }
}
