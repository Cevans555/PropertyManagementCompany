using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PropertyManagement.Data;

namespace PropertyManagement.IntegrationTests.Infrastructure;

public class PropertyManagementWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"PropertyManagement_Tests_{Guid.NewGuid():N}";

    private string ConnectionString =>
        $@"Server=(localdb)\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseContentRoot(FindWebProjectDirectory());
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
    }

    public virtual Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using (var scope = Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<PropertyManagementDbContext>().Database.EnsureDeletedAsync();
        }

        await DisposeAsync();
    }

    public HttpClient CreateBrowserClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    }

    private static string FindWebProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PropertyManagement.slnx")))
            directory = directory.Parent;

        if (directory is null)
            throw new InvalidOperationException("Could not find PropertyManagement.slnx above the test output folder.");

        return Path.Combine(directory.FullName, "src", "PropertyManagement.Web");
    }
}

[CollectionDefinition(Name)]
public sealed class WebCollection : ICollectionFixture<PropertyManagementWebFactory>
{
    public const string Name = "Web application";
}
