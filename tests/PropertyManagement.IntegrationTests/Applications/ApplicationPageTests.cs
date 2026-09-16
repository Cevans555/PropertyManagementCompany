using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Enums;
using PropertyManagement.IntegrationTests.Infrastructure;

namespace PropertyManagement.IntegrationTests;

[Collection(WebCollection.Name)]
public partial class ApplicationPageTests
{
    private const string Base64Placeholder = "AAAAAAAAAAA=";

    private readonly PropertyManagementWebFactory _factory;

    public ApplicationPageTests(PropertyManagementWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AvailableUnits_ListsOpenUnitsButNotLeasedOnes()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);
        var openUnitId = await _factory.CreateUnitAsync();
        var today = _factory.BusinessToday();
        var leasedUnitId = await _factory.QueryDbAsync(db =>
            db.Leases.Where(l => l.StartDate <= today && l.EndDate >= today).Select(l => l.UnitId).FirstAsync());

        var html = await client.GetStringAsync("/Applications/Available");

        Assert.Contains($"name=\"unitId\" value=\"{openUnitId}\"", html);
        Assert.DoesNotContain($"name=\"unitId\" value=\"{leasedUnitId}\"", html);
    }

    [Fact]
    public async Task Start_CreatesDraft_AndReopensItInsteadOfDuplicating()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);
        var unitId = await _factory.CreateUnitAsync();

        var first = await StartAsync(client, unitId);
        var second = await StartAsync(client, unitId);

        Assert.Equal(first, second);
        var status = await _factory.QueryDbAsync(db =>
            db.RentalApplications.Where(a => a.Id == first).Select(a => a.Status).SingleAsync());
        Assert.Equal(ApplicationStatus.Draft, status);
        Assert.Equal(1, await _factory.QueryDbAsync(db => db.RentalApplications.CountAsync(a => a.UnitId == unitId)));
    }

    [Fact]
    public async Task Details_IsNotFoundForAnotherApplicant()
    {
        var (_, applicationId) = await StartApplicationAsync();
        var otherClient = await _factory.CreateSignedInClientAsync(TestAccounts.OtherApplicant);

        var response = await otherClient.GetAsync($"/Applications/Details/{applicationId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Details_ManagerSeesReadOnlySummaryWithStatusHistory()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var returnedId = await _factory.QueryDbAsync(db =>
            db.RentalApplications.Where(a => a.Status == ApplicationStatus.Returned).Select(a => a.Id).FirstAsync());

        var html = await client.GetStringAsync($"/Applications/Details/{returnedId}");

        Assert.Contains("Status history", html);
        Assert.Contains("Returned", html);
        Assert.DoesNotContain("name=\"ApplicantDetails.FirstName\"", html);
        Assert.DoesNotContain("name=\"command\"", html);
    }

    [Fact]
    public async Task ApplicantPage_DoesNotShowStatusHistory()
    {
        var (client, applicationId) = await StartApplicationAsync();

        var html = await client.GetStringAsync($"/Applications/Details/{applicationId}?section=Summary");

        Assert.DoesNotContain("Status history", html);
    }

    [Fact]
    public async Task ApplicantSection_EditableForOwner()
    {
        var (client, applicationId) = await StartApplicationAsync();

        var html = await client.GetStringAsync($"/Applications/Details/{applicationId}");

        Assert.Contains("name=\"ApplicantDetails.FirstName\"", html);
        Assert.Contains($"value=\"{ApplicationCommandValues.Continue}\"", html);
    }

    [Fact]
    public async Task Continue_WithInvalidApplicantDetails_Returns422AndDoesNotSave()
    {
        var (client, applicationId) = await StartApplicationAsync();

        var response = await ContinueApplicantAsync(client, applicationId, firstName: "");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("First name is required.", html);
        Assert.Null(await DetailsSavedAtAsync(applicationId));
    }

    [Fact]
    public async Task Continue_WithValidApplicantDetails_SavesAndMovesToResidences()
    {
        var (client, applicationId) = await StartApplicationAsync();

        var response = await ContinueApplicantAsync(client, applicationId);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("section=Residences", response.Headers.Location!.OriginalString);
        Assert.NotNull(await DetailsSavedAtAsync(applicationId));
    }

    [Fact]
    public async Task Back_GoesToPreviousSectionWithoutSaving()
    {
        var (client, applicationId) = await StartApplicationAsync();

        var response = await PostCommandAsync(
            client, applicationId, "Residences", ApplicationCommandValues.Back, ApplicantFields("", "Changed"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("section=Applicant", response.Headers.Location!.OriginalString);
        Assert.Null(await DetailsSavedAtAsync(applicationId));
    }

    [Fact]
    public async Task ResidenceContinue_WithoutResidences_Returns422()
    {
        var (client, applicationId) = await StartApplicationAsync();

        var response = await PostCommandAsync(client, applicationId, "Residences", ApplicationCommandValues.Continue);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("Add at least one prior residence.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Submit_IsOnlyAcceptedFromSummary()
    {
        var (client, applicationId) = await StartApplicationAsync();

        var response = await PostCommandAsync(client, applicationId, "Applicant", ApplicationCommandValues.Submit);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WithIncompleteSections_Returns422()
    {
        var (client, applicationId) = await StartApplicationAsync();

        var response = await PostCommandAsync(client, applicationId, "Summary", ApplicationCommandValues.Submit);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(ApplicationStatus.Draft, await StatusAsync(applicationId));
    }

    [Fact]
    public async Task FullFlow_SaveBothSectionsAndSubmit_ThenPageIsReadOnly()
    {
        var (client, applicationId) = await StartApplicationAsync();

        Assert.Equal(HttpStatusCode.Redirect, (await ContinueApplicantAsync(client, applicationId)).StatusCode);

        var added = await AddResidenceAsync(client, applicationId);
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        Assert.Contains("Bob Landlord", await client.GetStringAsync($"/ApplicationResidences/List/{applicationId}"));

        var residencesSaved = await PostCommandAsync(client, applicationId, "Residences", ApplicationCommandValues.Continue);
        Assert.Equal(HttpStatusCode.Redirect, residencesSaved.StatusCode);
        Assert.Contains("section=Summary", residencesSaved.Headers.Location!.OriginalString);

        var summary = await client.GetStringAsync($"/Applications/Details/{applicationId}?section=Summary");
        Assert.DoesNotContain("Before you can submit", summary);

        var submitted = await PostCommandAsync(client, applicationId, "Summary", ApplicationCommandValues.Submit);
        Assert.Equal(HttpStatusCode.Redirect, submitted.StatusCode);
        Assert.Equal(ApplicationStatus.Submitted, await StatusAsync(applicationId));

        var readOnly = await client.GetStringAsync($"/Applications/Details/{applicationId}");
        Assert.Contains("Your application was submitted.", readOnly);
        Assert.DoesNotContain("name=\"ApplicantDetails.FirstName\"", readOnly);
        Assert.DoesNotContain("name=\"command\"", readOnly);

        var rejected = await client.PostFormWithTokenAsync(
            readOnly, $"/Applications/Details/{applicationId}", ApplicantFields(Base64Placeholder));
        Assert.Equal(HttpStatusCode.Redirect, rejected.StatusCode);
        Assert.Contains("/Account/AccessDenied", rejected.Headers.Location!.OriginalString);
        Assert.Equal(ApplicationStatus.Submitted, await StatusAsync(applicationId));
    }

    [Fact]
    public async Task Withdraw_ThroughModal_WithdrawsApplication()
    {
        var (client, applicationId) = await StartApplicationAsync();
        var url = $"/Applications/Withdraw/{applicationId}";

        var response = await client.PostFormAsync(url, url, new());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ApplicationStatus.Withdrawn, await StatusAsync(applicationId));
    }

    private static class ApplicationCommandValues
    {
        public const string Continue = "continue";
        public const string Back = "back";
        public const string Submit = "submit";
    }

    [GeneratedRegex(@"/Applications/Details/(\d+)")]
    private static partial Regex DetailsUrlPattern();

    private async Task<(HttpClient Client, int ApplicationId)> StartApplicationAsync(string email = TestAccounts.Applicant)
    {
        var client = await _factory.CreateSignedInClientAsync(email);
        var applicationId = await StartAsync(client, await _factory.CreateUnitAsync());
        return (client, applicationId);
    }

    private static async Task<int> StartAsync(HttpClient client, int unitId)
    {
        var response = await client.PostFormAsync("/Applications/Available", "/Applications/Start", new()
        {
            ["unitId"] = unitId.ToString(CultureInfo.InvariantCulture)
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var match = DetailsUrlPattern().Match(response.Headers.Location!.OriginalString);
        Assert.True(match.Success, $"Unexpected redirect: {response.Headers.Location}");
        return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static async Task<HttpResponseMessage> ContinueApplicantAsync(HttpClient client, int applicationId, string firstName = "Jane")
    {
        var html = await client.GetStringAsync($"/Applications/Details/{applicationId}?section=Applicant");
        var rowVersion = TestHelpers.HiddenValue(html, "ApplicantRowVersion");
        return await client.PostFormWithTokenAsync(html, $"/Applications/Details/{applicationId}", ApplicantFields(rowVersion, firstName));
    }

    private static async Task<HttpResponseMessage> AddResidenceAsync(HttpClient client, int applicationId)
    {
        var url = $"/ApplicationResidences/Create?applicationId={applicationId}";
        var html = await client.GetStringAsync(url);
        return await client.PostFormWithTokenAsync(html, url, new()
        {
            ["ResidenceSectionVersion"] = TestHelpers.HiddenValue(html, "ResidenceSectionVersion"),
            ["Street"] = "22 Elm St",
            ["City"] = "Troy",
            ["State"] = "NY",
            ["PostalCode"] = "12180",
            ["LandlordName"] = "Bob Landlord",
            ["LandlordPhone"] = "(518) 555-0199",
            ["MoveInDate"] = "2022-01-01",
            ["MoveOutDate"] = "2025-12-31"
        });
    }

    private static async Task<HttpResponseMessage> PostCommandAsync(
        HttpClient client, int applicationId, string section, string command, Dictionary<string, string>? extraFields = null)
    {
        var html = await client.GetStringAsync($"/Applications/Details/{applicationId}?section={section}");
        var fields = new Dictionary<string, string>(extraFields ?? []);
        if (html.Contains("name=\"ResidenceSectionVersion\""))
            fields["ResidenceSectionVersion"] = TestHelpers.HiddenValue(html, "ResidenceSectionVersion");

        fields["Section"] = section;
        fields["command"] = command;

        return await client.PostFormWithTokenAsync(html, $"/Applications/Details/{applicationId}", fields);
    }

    private static Dictionary<string, string> ApplicantFields(string rowVersion, string firstName = "Jane")
    {
        return new Dictionary<string, string>
        {
            ["Section"] = "Applicant",
            ["command"] = ApplicationCommandValues.Continue,
            ["ApplicantRowVersion"] = rowVersion,
            ["ApplicantDetails.FirstName"] = firstName,
            ["ApplicantDetails.LastName"] = "Doe",
            ["ApplicantDetails.Phone"] = "(518) 555-0100",
            ["ApplicantDetails.Email"] = "jane@example.com",
            ["ApplicantDetails.Street"] = "1 Main St",
            ["ApplicantDetails.City"] = "Albany",
            ["ApplicantDetails.State"] = "NY",
            ["ApplicantDetails.PostalCode"] = "12207"
        };
    }

    private Task<ApplicationStatus> StatusAsync(int applicationId)
    {
        return _factory.QueryDbAsync(db =>
            db.RentalApplications.Where(a => a.Id == applicationId).Select(a => a.Status).SingleAsync());
    }

    private Task<DateTime?> DetailsSavedAtAsync(int applicationId)
    {
        return _factory.QueryDbAsync(db => db.Applicants
            .Where(a => a.RentalApplicationId == applicationId && a.IsPrimary)
            .Select(a => a.DetailsSavedAt)
            .SingleAsync());
    }
}
