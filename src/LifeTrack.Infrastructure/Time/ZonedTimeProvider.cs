namespace LifeTrack.Infrastructure.Time;

public sealed class ZonedTimeProvider(TimeProvider inner, TimeZoneInfo zone) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => zone;

    public override DateTimeOffset GetUtcNow() => inner.GetUtcNow();
}
