namespace HudClock.Configuration;

/// <summary>
/// Options controlling player-stat lines on the main HUD. Both default to
/// off — these are survival-mode features and not every player cares about
/// numeric readouts.
/// </summary>
internal sealed class PlayerStatsOptions
{
    /// <summary>
    /// Show the player's current body temperature. Respects the Celsius/
    /// Fahrenheit toggle under <see cref="WeatherOptions.Fahrenheit"/> so
    /// world temp and body temp share the same unit.
    /// </summary>
    public bool ShowBodyTemperature { get; set; }

    /// <summary>
    /// Show current intoxication level as a percentage. The line is only
    /// visible when intoxication is greater than zero; toggling this on
    /// while sober has no immediate visible effect until the player drinks.
    /// </summary>
    public bool ShowIntoxication { get; set; }
}
