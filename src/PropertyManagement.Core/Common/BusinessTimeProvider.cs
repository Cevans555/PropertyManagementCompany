namespace PropertyManagement.Core.Common;

public sealed class BusinessTimeProvider(TimeZoneInfo timeZone) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => timeZone;
}