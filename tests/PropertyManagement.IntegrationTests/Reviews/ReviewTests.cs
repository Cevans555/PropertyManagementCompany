using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Enums;
using PropertyManagement.IntegrationTests.Infrastructure;

namespace PropertyManagement.IntegrationTests;

[Collection(WebCollection.Name)]
public class ReviewTests
{
    private readonly PropertyManagementWebFactory _factory;

    public ReviewTests(PropertyManagementWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Queue_ShowsSubmittedApplicationWithClaimAction()
    {
        var applicationId = await SubmittedApplicationAsync();
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var html = await client.GetStringAsync("/Reviews/Queue");

        Assert.Contains($"action=\"/Reviews/Claim/{applicationId}\"", html);
    }

    [Fact]
    public async Task Queue_IsDeniedToApplicants()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);

        var response = await client.GetAsync("/Reviews/Queue");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Claim_MovesToUnderReview_AndOpensApplication()
    {
        var applicationId = await SubmittedApplicationAsync();
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var response = await client.PostFormAsync($"/Applications/Details/{applicationId}", $"/Reviews/Claim/{applicationId}", new());

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains($"/Applications/Details/{applicationId}", response.Headers.Location!.OriginalString);
        var (status, claimedById) = await StatusAndClaimAsync(applicationId);
        Assert.Equal(ApplicationStatus.UnderReview, status);
        Assert.Equal(await _factory.UserIdAsync(TestAccounts.Manager), claimedById);

        var page = await client.GetStringAsync($"/Applications/Details/{applicationId}");
        Assert.Contains("Complete review", page);
        Assert.Contains("Release to queue", page);
    }

    [Fact]
    public async Task Claim_ByApplicant_IsDenied()
    {
        var applicationId = await SubmittedApplicationAsync();
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);

        var response = await client.PostFormAsync("/", $"/Reviews/Claim/{applicationId}", new());

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location!.OriginalString);
        Assert.Equal(ApplicationStatus.Submitted, (await StatusAndClaimAsync(applicationId)).Status);
    }

    [Fact]
    public async Task Release_PutsApplicationBackInQueue()
    {
        var applicationId = await ClaimedApplicationAsync();
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var response = await client.PostFormAsync($"/Applications/Details/{applicationId}", $"/Reviews/Release/{applicationId}", new());

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var (status, claimedById) = await StatusAndClaimAsync(applicationId);
        Assert.Equal(ApplicationStatus.Submitted, status);
        Assert.Null(claimedById);
    }

    [Fact]
    public async Task ClaimedByAnotherManager_ShowsClaimantAndRejectsTheirReview()
    {
        var applicationId = await ClaimedApplicationAsync();
        var otherManager = await _factory.CreateSignedInClientAsync(TestAccounts.OtherManager);

        var page = await otherManager.GetStringAsync($"/Applications/Details/{applicationId}");
        var response = await otherManager.PostFormWithTokenAsync(page, $"/Reviews/Review/{applicationId}", new()
        {
            ["Outcome"] = "Deny",
            ["Comment"] = "Trying to deny someone else's review"
        });

        Assert.Contains("Claimed by", page);
        Assert.DoesNotContain("Complete review", page);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("claimed by another property manager", await response.Content.ReadAsStringAsync());
        Assert.Equal(ApplicationStatus.UnderReview, (await StatusAndClaimAsync(applicationId)).Status);
    }

    [Theory]
    [InlineData("Return")]
    [InlineData("Deny")]
    public async Task Review_ReturnOrDenyWithoutComment_Returns422(string outcome)
    {
        var applicationId = await ClaimedApplicationAsync();
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var response = await PostReviewAsync(client, applicationId, outcome, comment: "");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("A comment is required to return or deny an application.", await response.Content.ReadAsStringAsync());
        Assert.Equal(ApplicationStatus.UnderReview, (await StatusAndClaimAsync(applicationId)).Status);
    }

    [Fact]
    public async Task Review_ApproveWithPastLeaseStart_Returns422()
    {
        var applicationId = await ClaimedApplicationAsync();
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var response = await PostReviewAsync(client, applicationId, "Approve", leaseStart: _factory.BusinessToday().AddDays(-1));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("can&#x27;t be in the past", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Review_Approve_CreatesLeaseOnChosenDate()
    {
        var applicationId = await ClaimedApplicationAsync();
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var leaseStart = _factory.BusinessToday().AddDays(10);

        var response = await PostReviewAsync(client, applicationId, "Approve", leaseStart: leaseStart);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ApplicationStatus.Approved, (await StatusAndClaimAsync(applicationId)).Status);
        var lease = await _factory.QueryDbAsync(db => db.Leases.AsNoTracking().SingleAsync(l => l.RentalApplicationId == applicationId));
        Assert.Equal(leaseStart, lease.StartDate);
        Assert.Equal(await _factory.UserIdAsync(TestAccounts.Manager), lease.CreatedById);
    }

    [Fact]
    public async Task Review_Deny_RecordsCommentInStatusHistory()
    {
        var applicationId = await ClaimedApplicationAsync();
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        const string comment = "Income could not be verified";

        var response = await PostReviewAsync(client, applicationId, "Deny", comment);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ApplicationStatus.Denied, (await StatusAndClaimAsync(applicationId)).Status);
        var page = await client.GetStringAsync($"/Applications/Details/{applicationId}");
        Assert.Contains(comment, page);
    }

    [Fact]
    public async Task Review_Return_LetsApplicantEditAgain()
    {
        var applicationId = await ClaimedApplicationAsync();
        var manager = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var response = await PostReviewAsync(manager, applicationId, "Return", "Please add your previous landlord");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ApplicationStatus.Returned, (await StatusAndClaimAsync(applicationId)).Status);
        var applicant = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);
        var page = await applicant.GetStringAsync($"/Applications/Details/{applicationId}");
        Assert.Contains("name=\"ApplicantDetails.FirstName\"", page);
    }

    [Fact]
    public async Task Notes_AddEditRemove_AndNeverShownToApplicant()
    {
        var applicationId = await SubmittedApplicationAsync();
        var manager = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var otherManager = await _factory.CreateSignedInClientAsync(TestAccounts.OtherManager);
        var applicant = await _factory.CreateSignedInClientAsync(TestAccounts.Applicant);
        var noteText = $"Internal note {Guid.NewGuid():N}";

        var addUrl = $"/ManagerNotes/Add?applicationId={applicationId}";
        Assert.Equal(HttpStatusCode.OK, (await manager.PostFormAsync(addUrl, addUrl, new() { ["Text"] = noteText })).StatusCode);
        Assert.Contains(noteText, await manager.GetStringAsync($"/ManagerNotes/List/{applicationId}"));
        Assert.Contains(noteText, await manager.GetStringAsync($"/Applications/Details/{applicationId}"));

        foreach (var section in new[] { "Applicant", "Residences", "Summary" })
            Assert.DoesNotContain(noteText, await applicant.GetStringAsync($"/Applications/Details/{applicationId}?section={section}"));

        var applicantNotes = await applicant.GetAsync($"/ManagerNotes/List/{applicationId}");
        Assert.Equal(HttpStatusCode.Redirect, applicantNotes.StatusCode);
        Assert.Contains("/Account/AccessDenied", applicantNotes.Headers.Location!.OriginalString);

        var noteId = await _factory.QueryDbAsync(db => db.ManagerNotes.Where(n => n.Text == noteText).Select(n => n.Id).SingleAsync());
        var editUrl = $"/ManagerNotes/Edit/{noteId}";
        Assert.Equal(HttpStatusCode.OK, (await otherManager.PostFormAsync(editUrl, editUrl, new() { ["Text"] = noteText + " (edited)" })).StatusCode);
        var edited = await _factory.QueryDbAsync(db => db.ManagerNotes.AsNoTracking().SingleAsync(n => n.Id == noteId));
        Assert.EndsWith("(edited)", edited.Text);
        Assert.Equal(await _factory.UserIdAsync(TestAccounts.Manager), edited.CreatedById);
        Assert.Equal(await _factory.UserIdAsync(TestAccounts.OtherManager), edited.ModifiedById);

        var deleteUrl = $"/ManagerNotes/Delete/{noteId}";
        Assert.Equal(HttpStatusCode.OK, (await manager.PostFormAsync(deleteUrl, deleteUrl, new())).StatusCode);
        Assert.False(await _factory.QueryDbAsync(db => db.ManagerNotes.AnyAsync(n => n.Id == noteId)));
    }

    [Fact]
    public async Task AddNote_Empty_Returns422()
    {
        var applicationId = await SubmittedApplicationAsync();
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);
        var url = $"/ManagerNotes/Add?applicationId={applicationId}";

        var response = await client.PostFormAsync(url, url, new() { ["Text"] = "" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Review_InvalidOutcome_IsRejectedAndDoesNotChangeStatus()
    {
        var applicationId = await ClaimedApplicationAsync();
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var response = await PostReviewAsync(client, applicationId, outcome: "999", comment: "Tampered outcome");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(ApplicationStatus.UnderReview, (await StatusAndClaimAsync(applicationId)).Status);
    }

    private async Task<int> SubmittedApplicationAsync()
    {
        return await _factory.CreateApplicationAsync(
            await _factory.CreateUnitAsync(), await _factory.UserIdAsync(TestAccounts.Applicant));
    }

    private async Task<int> ClaimedApplicationAsync()
    {
        return await _factory.CreateApplicationAsync(
            await _factory.CreateUnitAsync(),
            await _factory.UserIdAsync(TestAccounts.Applicant),
            claimedBy: await _factory.UserIdAsync(TestAccounts.Manager));
    }

    private Task<(ApplicationStatus Status, string? ClaimedById)> StatusAndClaimAsync(int applicationId)
    {
        return _factory.QueryDbAsync(async db =>
        {
            var row = await db.RentalApplications
                .Where(a => a.Id == applicationId)
                .Select(a => new { a.Status, a.ClaimedById })
                .SingleAsync();
            return (row.Status, row.ClaimedById);
        });
    }

    private Task<HttpResponseMessage> PostReviewAsync(
        HttpClient client, int applicationId, string outcome, string comment = "", DateOnly? leaseStart = null)
    {
        var url = $"/Reviews/Review/{applicationId}";
        return client.PostFormAsync(url, url, new()
        {
            ["Outcome"] = outcome,
            ["Comment"] = comment,
            ["LeaseStartDate"] = (leaseStart ?? _factory.BusinessToday()).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        });
    }
}
