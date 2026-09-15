namespace PropertyManagement.Core.Common;

public sealed class BusinessTimeProvider : TimeProvider
{
    private readonly TimeZoneInfo _timeZone;

    public BusinessTimeProvider(TimeZoneInfo timeZone)
    {
        _timeZone = timeZone;
    }

    public override TimeZoneInfo LocalTimeZone => _timeZone;
}
