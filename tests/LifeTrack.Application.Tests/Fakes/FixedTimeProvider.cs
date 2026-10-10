namespace LifeTrack.Application.Tests.Fakes;

public sealed class FixedTimeProvider(DateOnly today) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() =>
        new(today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
