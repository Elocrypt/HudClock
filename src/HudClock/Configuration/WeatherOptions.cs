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
}
