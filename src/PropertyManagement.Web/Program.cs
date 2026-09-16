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
builder.Services.AddScoped<PropertyService>();
builder.Services.AddScoped<ManagerNoteService>();
builder.Services.AddScoped<ApplicationPageBuilder>();

builder.Services.AddDbContext<PropertyManagementDbContext>((services, options) =>
{
    // Read when the context is created, so integration tests can point the app at their own database.
    var connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("PropertyManagement.Data"));
    options.AddInterceptors(services.GetRequiredService<AuditSaveChangesInterceptor>());
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        // Required: AddDefaultIdentity applied this when the schema was scaffolded. Removing it widens
        // Identity's composite key columns to nvarchar(450) and the app won't start.
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
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(Policies.PropertyManager, policy => policy.RequireRole(Roles.PropertyManager))
    .AddPolicy(Policies.Applicant, policy => policy.RequireRole(Roles.Applicant));

builder.Services.AddSingleton<IAuthorizationHandler, RentalApplicationAuthorizationHandler>();

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

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program
{
}
