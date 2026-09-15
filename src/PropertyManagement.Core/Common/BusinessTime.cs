namespace PropertyManagement.Core.Common;

public sealed class BusinessTimeProvider(TimeZoneInfo timeZone) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => timeZone;
}

public static class TimeProviderExtensions
{
    public static DateOnly Today(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
}