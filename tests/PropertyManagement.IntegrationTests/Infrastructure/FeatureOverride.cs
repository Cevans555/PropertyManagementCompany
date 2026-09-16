using Microsoft.Extensions.Configuration;

namespace PropertyManagement.IntegrationTests.Infrastructure;

public sealed class FeatureOverride : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly string _key;
    private readonly string? _previousValue;

    public FeatureOverride(IConfiguration configuration, string key, string value)
    {
        _configuration = configuration;
        _key = key;
        _previousValue = configuration[key];
        configuration[key] = value;
    }

    public void Dispose()
    {
        _configuration[_key] = _previousValue;
    }
}
