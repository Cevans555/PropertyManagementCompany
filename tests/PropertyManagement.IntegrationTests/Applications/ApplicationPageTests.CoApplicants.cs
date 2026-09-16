using System.Net;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Enums;
using PropertyManagement.IntegrationTests.Infrastructure;

namespace PropertyManagement.IntegrationTests;

// Bonus 5: more than one applicant on an application. Any of them can view and edit it, and every one of
// them has to save their own applicant information before the application can be submitted.
public partial class ApplicationPageTests
{
    [Fact]
    public async Task CoApplicant_CanViewAndEdit_AndMustSaveDetailsBeforeSubmit()
    {
        var (client, applicationId) = await StartApplicationAsync();
        await ContinueApplicantAsync(client, applicationId);
        await AddResidenceAsync(client, applicationId);
        await PostCommandAsync(client, applicationId, "Residences", ApplicationCommandValues.Continue);

        var addUrl = $"/ApplicationApplicants/Add?applicationId={applicationId}";
        var added = await client.PostFormAsync(addUrl, addUrl, new() { ["Email"] = TestAccounts.OtherApplicant });
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);

        var coApplicant = await _factory.CreateSignedInClientAsync(TestAccounts.OtherApplicant);
        Assert.Equal(HttpStatusCode.OK, (await coApplicant.GetAsync($"/Applications/Details/{applicationId}")).StatusCode);

        var blocked = await PostCommandAsync(client, applicationId, "Summary", ApplicationCommandValues.Submit);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blocked.StatusCode);
        Assert.Contains("every applicant", await blocked.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Redirect, (await ContinueApplicantAsync(coApplicant, applicationId)).StatusCode);
        var submitted = await PostCommandAsync(client, applicationId, "Summary", ApplicationCommandValues.Submit);
        Assert.Equal(HttpStatusCode.Redirect, submitted.StatusCode);
        Assert.Equal(ApplicationStatus.Submitted, await StatusAsync(applicationId));
    }

    [Fact]
    public async Task AddCoApplicant_WithUnknownEmail_Returns422()
    {
        var (client, applicationId) = await StartApplicationAsync();
        var addUrl = $"/ApplicationApplicants/Add?applicationId={applicationId}";

        var response = await client.PostFormAsync(addUrl, addUrl, new() { ["Email"] = "nobody@example.com" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("No applicant account uses that email address.", await response.Content.ReadAsStringAsync());
    }
}
