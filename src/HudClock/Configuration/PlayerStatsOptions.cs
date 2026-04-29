namespace HudClock.Configuration;

/// <summary>
/// Options controlling player-stat lines on the main HUD. All default to
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
    /// <remarks>
    /// Vanilla path: line is shown only when the player drops below normal
    /// body temperature (cool / FREEZING). When an immersive-temperature
    /// mod is detected (presence of the <c>apparentTemp</c> watched
    /// attribute), the line extends symmetrically above normal as
    /// well (warm / HOT). See the integration contract in
    /// <c>docs/integration.md</c>.
    /// </remarks>
    public bool ShowBodyTemperature { get; set; }

    /// <summary>
    /// Show current intoxication level as a percentage. The line is only
    /// visible when intoxication is greater than zero; toggling this on
    /// while sober has no immediate visible effect until the player drinks.
    /// </summary>
    public bool ShowIntoxication { get; set; }

    /// <summary>
    /// Show the apparent ("felt") temperature provided by an immersive-
    /// temperature mod. Off by default and silently hidden when no mod is
    /// providing the underlying <c>apparentTempC</c> watched attribute, so
    /// turning this on without a compatible mod has no effect rather than
    /// surfacing a permanently empty line.
    /// </summary>
    public bool ShowApparentTemperature { get; set; }
}
