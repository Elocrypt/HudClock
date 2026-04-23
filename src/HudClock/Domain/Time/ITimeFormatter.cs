namespace HudClock.Domain.Time;

/// <summary>
/// Pure formatter that converts an (hour, minute) pair into a display string.
/// No dependencies on the game API; safe to unit-test in isolation.
/// </summary>
internal interface ITimeFormatter
{
    /// <summary>
    /// Format a 24-hour-clock hour and minute into the configured display format.
    /// </summary>
    /// <param name="hour24">Hour of day in 24-hour form (0-23).</param>
    /// <param name="minute">Minute of hour (0-59).</param>
    string Format(int hour24, int minute);
}
