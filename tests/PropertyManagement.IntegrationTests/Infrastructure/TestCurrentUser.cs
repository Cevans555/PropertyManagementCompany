using PropertyManagement.Data.Auditing;

namespace PropertyManagement.IntegrationTests.Infrastructure;

internal sealed class TestCurrentUser : ICurrentUser
{
    public string? UserId { get; set; } = "test-user";
}
