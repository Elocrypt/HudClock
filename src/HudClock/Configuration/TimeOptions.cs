namespace HudClock.Configuration;

/// <summary>Clock format for both in-game time and real-time lines.</summary>
internal enum TimeFormat
{
    TwelveHour,
    TwentyFourHour,
}

/// <summary>Options controlling in-game time, date, and real-time display.</summary>
internal sealed class TimeOptions
{
    public bool ShowDate { get; set; } = true;
    public bool ShowTime { get; set; } = true;
    public bool ShowRealtime { get; set; }
    public TimeFormat Format { get; set; } = TimeFormat.TwentyFourHour;
}
