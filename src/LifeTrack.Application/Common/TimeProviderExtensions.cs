namespace LifeTrack.Application.Common;

public static class TimeProviderExtensions
{
    public static DateOnly Today(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
}
