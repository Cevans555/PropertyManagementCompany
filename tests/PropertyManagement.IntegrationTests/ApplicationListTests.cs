using System.Net;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.IntegrationTests.Infrastructure;

namespace PropertyManagement.IntegrationTests;

[Collection(WebCollection.Name)]
public class ApplicationListTests
{
    private readonly PropertyManagementWebFactory _factory;

    public ApplicationListTests(PropertyManagementWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_RedirectsAnonymousUserToLogin()
    {
        var response = await _factory.CreateBrowserClient().GetAsync("/Applications");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Applicant_SeesOnlyApplicationsTheyAreOn()
    {
        var own = await CreateApplicationAsync(TestAccounts.Applicant);
        var someoneElses = await CreateApplicationAsync(TestAccounts.OtherApplicant);
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);

        var html = await client.GetStringAsync("/Applications");

        Assert.Contains(DetailsLink(own), html);
        Assert.DoesNotContain(DetailsLink(someoneElses), html);
        Assert.DoesNotContain("Claimed by", html);
    }

    [Fact]
    public async Task Manager_SeesEveryonesApplications()
    {
        var first = await CreateApplicationAsync(TestAccounts.Applicant);
        var second = await CreateApplicationAsync(TestAccounts.OtherApplicant);
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var html = await client.GetStringAsync("/Applications");

        Assert.Contains(DetailsLink(first), html);
        Assert.Contains(DetailsLink(second), html);
        Assert.Contains("Claimed by", html);
    }

    [Fact]
    public async Task StatusFilter_ShowsOnlyThatStatus()
    {
        var draft = await CreateApplicationAsync(TestAccounts.Applicant, submit: false);
        var submitted = await CreateApplicationAsync(TestAccounts.Applicant, submit: true);
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var drafts = await client.GetStringAsync("/Applications?status=Draft");
        var submittedOnly = await client.GetStringAsync("/Applications?status=Submitted");

        Assert.Contains(DetailsLink(draft), drafts);
        Assert.DoesNotContain(DetailsLink(submitted), drafts);
        Assert.Contains(DetailsLink(submitted), submittedOnly);
        Assert.DoesNotContain(DetailsLink(draft), submittedOnly);
        Assert.Contains("<option selected=\"selected\" value=\"Submitted\">", submittedOnly);
    }

    [Fact]
    public async Task PropertyFilter_ShowsOnlyThatProperty()
    {
        var inProperty = await CreateApplicationAsync(TestAccounts.Applicant);
        var elsewhere = await CreateApplicationAsync(TestAccounts.Applicant);
        var propertyId = await _factory.QueryDbAsync(db =>
            db.RentalApplications.Where(a => a.Id == inProperty).Select(a => a.Unit.PropertyId).SingleAsync());
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);

        var html = await client.GetStringAsync($"/Applications?propertyId={propertyId}");

        Assert.Contains(DetailsLink(inProperty), html);
        Assert.DoesNotContain(DetailsLink(elsewhere), html);
    }

    [Fact]
    public async Task CombinedFilters_WithNoMatches_ShowsEmptyState()
    {
        var submitted = await CreateApplicationAsync(TestAccounts.Applicant, submit: true);
        var propertyId = await _factory.QueryDbAsync(db =>
            db.RentalApplications.Where(a => a.Id == submitted).Select(a => a.Unit.PropertyId).SingleAsync());
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var html = await client.GetStringAsync($"/Applications?status=Approved&propertyId={propertyId}");

        Assert.Contains("No applications match these filters.", html);
    }

    [Fact]
    public async Task InvalidFilterValue_IsIgnored()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var response = await client.GetAsync("/Applications?status=NotAStatus");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static string DetailsLink(int applicationId)
    {
        return $"href=\"/Applications/Details/{applicationId}\"";
    }

    private async Task<int> CreateApplicationAsync(string applicantEmail, bool submit = true)
    {
        return await _factory.CreateApplicationAsync(
            await _factory.CreateUnitAsync(), await _factory.UserIdAsync(applicantEmail), submit);
    }
}
