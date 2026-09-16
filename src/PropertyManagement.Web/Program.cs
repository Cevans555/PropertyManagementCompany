using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common;
using PropertyManagement.Core.Security;
using PropertyManagement.Data;
using PropertyManagement.Data.Auditing;
using PropertyManagement.Data.Queries;
using PropertyManagement.Data.Seeding;
using PropertyManagement.Data.Services;
using PropertyManagement.Web.Authorization;
using PropertyManagement.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

var businessTimeZone = TimeZoneInfo.FindSystemTimeZoneById(builder.Configuration["Business:TimeZone"] ?? "America/New_York");
builder.Services.AddSingleton<TimeProvider>(new BusinessTimeProvider(businessTimeZone));

builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddScoped<LeaseQueries>();
builder.Services.AddScoped<ApplicationUpdater>();
builder.Services.AddScoped<RentalApplicationService>();
builder.Services.AddScoped<ApplicationReviewService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<PropertyManagementDbContext>((services, options) =>
{
    options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("PropertyManagement.Data"));
    options.AddInterceptors(services.GetRequiredService<AuditSaveChangesInterceptor>());
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity without its Razor Pages UI: registering, logging in and out are MVC (AccountController).
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
    {
        // There's no email sending in this app, so accounts are usable immediately.
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        // Matches what AddDefaultIdentity applied when the schema was created, so the model still agrees
        // with the migration. It sets the length of Identity's composite key columns (LoginProvider,
        // ProviderKey, Name); without it they'd widen to nvarchar(450) and need a pointless migration.
        options.Stores.MaxLengthForKeys = 128;
    })
    .AddEntityFrameworkStores<PropertyManagementDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAuthorizationBuilder()
    // Every endpoint needs a signed-in user unless it opts out with [AllowAnonymous].
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(Policies.PropertyManager, policy => policy.RequireRole(Roles.PropertyManager))
    .AddPolicy(Policies.Applicant, policy => policy.RequireRole(Roles.Applicant));

// Decides what a user may do with one specific application (view, edit, withdraw, review, notes).
builder.Services.AddSingleton<IAuthorizationHandler, RentalApplicationAuthorizationHandler>();

// Every POST is checked for an antiforgery token, without repeating the attribute on each action.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// CSS and JS have to load on the login page, before anyone is signed in.
app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

/// <summary>Exposed so integration tests can host the app with WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program
{
}
