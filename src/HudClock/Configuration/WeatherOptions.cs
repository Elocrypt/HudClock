namespace HudClock.Configuration;

/// <summary>How the wind line is rendered, or whether it's hidden.</summary>
internal enum WindDisplay
{
    /// <summary>Show the Beaufort scale name (e.g. "gentle breeze").</summary>
    BeaufortText,

    /// <summary>Show wind strength as a percentage of maximum.</summary>
    Percentage,

    /// <summary>Hide the wind line entirely.</summary>
    Hidden,
}

/// <summary>Options controlling season, temperature, and wind display.</summary>
internal sealed class WeatherOptions
{
    public bool ShowSeason { get; set; } = true;
    public bool ShowTemperature { get; set; } = true;
    public bool Fahrenheit { get; set; }
    public WindDisplay Wind { get; set; } = WindDisplay.BeaufortText;

    /// <summary>
    /// Show the <i>current</i> precipitation level at the player position (how
    /// hard it is raining right now), labelled "Current Rainfall". Off by
    /// default. Distinct from <see cref="ShowAreaRainfall"/>.
    /// </summary>
    public bool ShowRainfall { get; set; }

    /// <summary>
    /// Show the <i>area</i> (world-generation) rainfall — the biome's baseline
    /// wetness — using the same wording the vanilla Environment dialog uses
    /// ("Rainfall in this Area", with the vanilla freq-* descriptors). Off by
    /// default.
    /// </summary>
    public bool ShowAreaRainfall { get; set; }
}
