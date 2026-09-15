using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Core.Common;
using PropertyManagement.Data;
using PropertyManagement.Data.Auditing;
using PropertyManagement.Data.Seeding;
using PropertyManagement.Data.Services;
using PropertyManagement.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

var businessTimeZone = TimeZoneInfo.FindSystemTimeZoneById(builder.Configuration["Business:TimeZone"] ?? "America/New_York");
builder.Services.AddSingleton<TimeProvider>(new BusinessTimeProvider(businessTimeZone));

builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddScoped<LeaseAvailabilityQuery>();
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

builder.Services.AddDefaultIdentity<AppUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<PropertyManagementDbContext>();
builder.Services.AddControllersWithViews();

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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
