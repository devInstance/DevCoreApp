using DevInstance.DevCoreApp.Client.Services.Core.Time;
using Xunit;

namespace DevInstance.DevCoreApp.Client.Services.Tests.Core;

public class LocalTimeServiceTests
{
    private static LocalTimeService NewYork()
    {
        var service = new LocalTimeService();
        service.SetTimeZone("America/New_York");
        return service;
    }

    [Fact]
    public void utc_is_converted_to_profile_zone_including_dst()
    {
        var time = NewYork();

        var summer = time.ToLocal(new DateTime(2026, 7, 1, 16, 0, 0, DateTimeKind.Utc));
        var winter = time.ToLocal(new DateTime(2026, 1, 15, 16, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2026, 7, 1, 12, 0, 0), summer);   // EDT, UTC-4
        Assert.Equal(new DateTime(2026, 1, 15, 11, 0, 0), winter);  // EST, UTC-5
        Assert.Equal(DateTimeKind.Unspecified, summer.Kind);
    }

    [Fact]
    public void unspecified_input_is_treated_as_utc()
    {
        var time = NewYork();

        var local = time.ToLocal(new DateTime(2026, 7, 1, 16, 0, 0, DateTimeKind.Unspecified));

        Assert.Equal(new DateTime(2026, 7, 1, 12, 0, 0), local);
    }

    [Fact]
    public void local_input_round_trips_to_utc()
    {
        var time = NewYork();

        var utc = time.ToUtc(new DateTime(2026, 7, 1, 12, 0, 0));

        Assert.Equal(new DateTime(2026, 7, 1, 16, 0, 0, DateTimeKind.Utc), utc);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
    }

    [Fact]
    public void time_in_dst_gap_does_not_throw()
    {
        var time = NewYork();

        // 2026-03-08 02:30 never happens in New York (clocks jump 02:00 -> 03:00).
        var utc = time.ToUtc(new DateTime(2026, 3, 8, 2, 30, 0));

        Assert.Equal(new DateTime(2026, 3, 8, 7, 30, 0, DateTimeKind.Utc), utc);
    }

    [Fact]
    public void unknown_or_empty_zone_falls_back_to_local_and_raises_changed_once()
    {
        var time = NewYork();
        var changes = 0;
        time.Changed += () => changes++;

        time.SetTimeZone("Not/AZone");
        time.SetTimeZone(null);

        Assert.Equal(TimeZoneInfo.Local.Id, time.TimeZone.Id);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void format_uses_zone_and_empty_for_null()
    {
        var time = NewYork();

        Assert.Equal("2026-07-01 12:00", time.Format(new DateTime(2026, 7, 1, 16, 0, 0, DateTimeKind.Utc), "yyyy-MM-dd HH:mm"));
        Assert.Equal("-", time.Format(null, empty: "-"));
    }
}
