namespace PropertyManagement.Core.Common;

public static class TimeProviderExtensions
{
    public static DateOnly Today(this TimeProvider timeProvider)
    {
        return DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
    }
}