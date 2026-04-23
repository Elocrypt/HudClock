using HudClock.Configuration;
using HudClock.Domain.Time;

namespace HudClock.Tests.Domain.Time;

public class TimeFormatterTests
{
    // Stub translator that returns the key itself — lets tests assert on the
    // unambiguous lang keys without a live game runtime.
    private static readonly System.Func<string, string> Identity = k => k;

    private static TimeFormatter Make(TimeFormat format)
        => new(new TimeOptions { Format = format }, Identity);

    // --- 24-hour format ---

    [Theory]
    [InlineData(0, 0, "00:00")]
    [InlineData(9, 5, "09:05")]
    [InlineData(12, 0, "12:00")]
    [InlineData(13, 45, "13:45")]
    [InlineData(23, 59, "23:59")]
    public void Format_24h_renders_zero_padded_pairs(int h, int m, string expected)
    {
        Assert.Equal(expected, Make(TimeFormat.TwentyFourHour).Format(h, m));
    }

    // --- 12-hour format ---

    [Theory]
    [InlineData(0, 0, "12:00 hudclock:am")]   // midnight -> 12 AM
    [InlineData(1, 15, "01:15 hudclock:am")]
    [InlineData(11, 59, "11:59 hudclock:am")]
    [InlineData(12, 0, "12:00 hudclock:pm")]  // noon -> 12 PM
    [InlineData(12, 30, "12:30 hudclock:pm")]
    [InlineData(13, 0, "01:00 hudclock:pm")]
    [InlineData(23, 59, "11:59 hudclock:pm")]
    public void Format_12h_handles_midnight_noon_and_pm_boundary(int h, int m, string expected)
    {
        Assert.Equal(expected, Make(TimeFormat.TwelveHour).Format(h, m));
    }

    // --- Input normalization ---

    [Fact]
    public void Format_24h_normalizes_hour_overflow()
    {
        // Calendar floor can legitimately produce hour == 24 at exact day-boundary transitions.
        Assert.Equal("00:00", Make(TimeFormat.TwentyFourHour).Format(24, 0));
    }

    [Fact]
    public void Format_24h_normalizes_negative_hour()
    {
        Assert.Equal("23:00", Make(TimeFormat.TwentyFourHour).Format(-1, 0));
    }

    [Fact]
    public void Format_24h_normalizes_minute_overflow()
    {
        Assert.Equal("10:00", Make(TimeFormat.TwentyFourHour).Format(10, 60));
    }

    // --- Translator contract ---

    [Fact]
    public void Format_12h_invokes_translator_for_AM_key_before_noon()
    {
        string? requested = null;
        var formatter = new TimeFormatter(
            new TimeOptions { Format = TimeFormat.TwelveHour },
            k => { requested = k; return "AM"; });

        formatter.Format(9, 0);

        Assert.Equal(TimeFormatter.AmKey, requested);
    }

    [Fact]
    public void Format_12h_invokes_translator_for_PM_key_at_or_after_noon()
    {
        string? requested = null;
        var formatter = new TimeFormatter(
            new TimeOptions { Format = TimeFormat.TwelveHour },
            k => { requested = k; return "PM"; });

        formatter.Format(12, 0);

        Assert.Equal(TimeFormatter.PmKey, requested);
    }

    // --- Constructor guards ---

    [Fact]
    public void Constructor_rejects_null_options()
    {
        Assert.Throws<System.ArgumentNullException>(() => new TimeFormatter(null!, Identity));
    }

    [Fact]
    public void Constructor_rejects_null_translator()
    {
        Assert.Throws<System.ArgumentNullException>(() => new TimeFormatter(new TimeOptions(), null!));
    }
}
