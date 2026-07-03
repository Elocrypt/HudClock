using HudClock.Domain.Weather;

namespace HudClock.Tests.Domain.Weather;

public class RainfallFrequencyTests
{
    // Thresholds replicate the vanilla Environment dialog's value->word table
    // (VS wiki, Weather page). These tests lock the boundaries so any drift
    // surfaces at build time; if a future VS version moves them, update both
    // the helper and this table together after an in-game check.
    [Theory]
    [InlineData(0.00f, "veryrare")]
    [InlineData(0.14f, "veryrare")]
    [InlineData(0.15f, "rarely")]
    [InlineData(0.29f, "rarely")]
    [InlineData(0.30f, "uncommon")]
    [InlineData(0.44f, "uncommon")]
    [InlineData(0.45f, "common")]
    [InlineData(0.69f, "common")]
    [InlineData(0.70f, "verycommon")]
    [InlineData(0.89f, "verycommon")]
    [InlineData(0.90f, "allthetime")]
    [InlineData(1.00f, "allthetime")]
    public void KeySuffix_maps_to_vanilla_freq_bucket(float rainfall, string expected)
    {
        Assert.Equal(expected, RainfallFrequency.KeySuffix(rainfall));
    }
}
