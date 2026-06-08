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

    /// <summary>
    /// Show the current lunar phase as its own line. Off by default — a new
    /// optional line follows the same opt-in convention as rainfall and the
    /// player-stat lines, so existing users don't gain an unexpected line on
    /// update.
    /// </summary>
    public bool ShowMoonPhase { get; set; }

    public TimeFormat Format { get; set; } = TimeFormat.TwentyFourHour;
}
