using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyManagement.Core.Security;
using PropertyManagement.Data;
using PropertyManagement.Data.Seeding;
using PropertyManagement.IntegrationTests.Infrastructure;

namespace PropertyManagement.IntegrationTests;

[Collection(WebCollection.Name)]
public class AccountTests
{
    private readonly PropertyManagementWebFactory _factory;

    public AccountTests(PropertyManagementWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_AsApplicant_CreatesTheUserInTheApplicantRole()
    {
        var client = _factory.CreateBrowserClient();
        var email = UniqueEmail();

        var response = await client.PostFormAsync("/Account/Register", "/Account/Register", RegisterFields(email, Roles.Applicant));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal([Roles.Applicant], await RolesAsync(email));
    }

    [Fact]
    public async Task Register_AsPropertyManager_CreatesTheUserInThePropertyManagerRole()
    {
        var client = _factory.CreateBrowserClient();
        var email = UniqueEmail();

        var response = await client.PostFormAsync("/Account/Register", "/Account/Register", RegisterFields(email, Roles.PropertyManager));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal([Roles.PropertyManager], await RolesAsync(email));
    }

    [Fact]
    public async Task Register_SignsTheNewUserIn()
    {
        var client = _factory.CreateBrowserClient();
        var email = UniqueEmail();

        await client.PostFormAsync("/Account/Register", "/Account/Register", RegisterFields(email, Roles.PropertyManager));
        var response = await client.GetAsync("/Properties");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithAnUnknownRole_IsRejectedAndCreatesNoUser()
    {
        var client = _factory.CreateBrowserClient();
        var email = UniqueEmail();

        var response = await client.PostFormAsync(
            "/Account/Register", "/Account/Register", RegisterFields(email, "Administrator"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("applicant or a property manager", await response.Content.ReadAsStringAsync());
        Assert.False(await _factory.QueryDbAsync(db => db.Users.AnyAsync(u => u.Email == email)));
    }

    [Fact]
    public async Task Register_WithAnEmailAlreadyInUse_IsRejected()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.PostFormAsync(
            "/Account/Register", "/Account/Register", RegisterFields(TestAccounts.Applicant, Roles.Applicant));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([Roles.Applicant], await RolesAsync(TestAccounts.Applicant));
    }

    [Fact]
    public async Task Login_WithValidCredentials_AuthenticatesTheUser()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.PostFormAsync("/Account/Login", "/Account/Login", new()
        {
            ["Email"] = TestAccounts.Manager,
            ["Password"] = DbInitializer.DemoPassword
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Properties")).StatusCode);
    }

    [Fact]
    public async Task Login_WithTheWrongPassword_IsRefusedAndLeavesTheUserAnonymous()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.PostFormAsync("/Account/Login", "/Account/Login", new()
        {
            ["Email"] = TestAccounts.Manager,
            ["Password"] = "NotThePassword1!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid email or password.", await response.Content.ReadAsStringAsync());

        var protectedPage = await client.GetAsync("/Properties");
        Assert.Equal(HttpStatusCode.Redirect, protectedPage.StatusCode);
        Assert.Contains("/Account/Login", protectedPage.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Login_HonoursALocalReturnUrl()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.PostFormAsync("/Account/Login", "/Account/Login", new()
        {
            ["Email"] = TestAccounts.Manager,
            ["Password"] = DbInitializer.DemoPassword,
            ["ReturnUrl"] = "/Properties"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Properties", response.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("https://evil.example.com/steal")]
    [InlineData("//evil.example.com/steal")]
    public async Task Login_DoesNotHonourAnExternalReturnUrl(string returnUrl)
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.PostFormAsync("/Account/Login", "/Account/Login", new()
        {
            ["Email"] = TestAccounts.Manager,
            ["Password"] = DbInitializer.DemoPassword,
            ["ReturnUrl"] = returnUrl
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.DoesNotContain("evil.example.com", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task ProtectedPage_RedirectsAnonymousUserToLoginWithAReturnUrl()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.GetAsync("/Applications");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.OriginalString;
        Assert.Contains("/Account/Login", location);
        Assert.Contains("ReturnUrl=%2FApplications", location);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var client = await _factory.CreateSignedInClientAsync(TestAccounts.Manager);

        var page = await client.GetStringAsync("/Properties");
        var response = await client.PostFormWithTokenAsync(page, "/Account/Logout", []);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var protectedPage = await client.GetAsync("/Properties");
        Assert.Equal(HttpStatusCode.Redirect, protectedPage.StatusCode);
        Assert.Contains("/Account/Login", protectedPage.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Login_PostedWithoutAnAntiforgeryToken_IsRejected()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = TestAccounts.Manager,
            ["Password"] = DbInitializer.DemoPassword
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Dictionary<string, string> RegisterFields(string email, string role)
    {
        return new Dictionary<string, string>
        {
            ["Role"] = role,
            ["FirstName"] = "New",
            ["LastName"] = "Person",
            ["Email"] = email,
            ["Password"] = DbInitializer.DemoPassword,
            ["ConfirmPassword"] = DbInitializer.DemoPassword
        };
    }

    private static string UniqueEmail()
    {
        return $"new-{Guid.NewGuid():N}@demo.com";
    }

    private async Task<IList<string>> RolesAsync(string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await users.FindByEmailAsync(email);
        Assert.NotNull(user);
        return await users.GetRolesAsync(user);
    }
}
