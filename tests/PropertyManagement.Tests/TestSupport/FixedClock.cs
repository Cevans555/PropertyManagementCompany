namespace PropertyManagement.Tests.TestSupport;

internal sealed class FixedClock : TimeProvider
{
    private readonly DateTimeOffset _utcNow;
    private readonly TimeZoneInfo _zone;

    public FixedClock(DateTimeOffset utcNow, TimeZoneInfo zone)
    {
        _utcNow = utcNow;
        _zone = zone;
    }

    public override TimeZoneInfo LocalTimeZone => _zone;

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }
}
