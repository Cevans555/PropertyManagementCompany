using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using PropertyManagement.IntegrationTests.Infrastructure;

namespace PropertyManagement.BrowserTests.Infrastructure;

public sealed class KestrelWebFactory : PropertyManagementWebFactory
{
    public string BaseUrl { get; private set; } = string.Empty;

    public KestrelWebFactory()
    {
        UseKestrel(0);
    }

    public override Task InitializeAsync()
    {
        StartServer();

        var addresses = Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("The test server didn't report its listening address.");

        BaseUrl = addresses.Addresses.First(address => address.StartsWith("http://", StringComparison.Ordinal));
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // The "Testing" environment doesn't enable build-time static web assets, so without this every script and
        // stylesheet comes back as an empty 200 and no client-side code runs.
        builder.UseStaticWebAssets();
    }
}
