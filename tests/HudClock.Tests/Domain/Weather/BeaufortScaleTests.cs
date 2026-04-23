using HudClock.Domain.Weather;

namespace HudClock.Tests.Domain.Weather;

public class BeaufortScaleTests
{
    // Each pair: (wind speed, expected Beaufort level). Boundaries were taken
    // verbatim from the 3.x if-else chain; these tests lock them in so any
    // accidental change surfaces at build time.
    [Theory]
    [InlineData(0.00, 0)]   // calm
    [InlineData(0.01, 0)]   // below the first threshold
    [InlineData(0.02, 1)]   // exactly at threshold[0]
    [InlineData(0.05, 1)]
    [InlineData(0.06, 2)]
    [InlineData(0.11, 2)]
    [InlineData(0.12, 3)]
    [InlineData(0.19, 3)]
    [InlineData(0.20, 4)]
    [InlineData(0.29, 4)]
    [InlineData(0.30, 5)]
    [InlineData(0.39, 5)]
    [InlineData(0.40, 6)]
    [InlineData(0.50, 6)]
    [InlineData(0.51, 7)]
    [InlineData(0.61, 7)]
    [InlineData(0.62, 8)]
    [InlineData(0.74, 8)]
    [InlineData(0.75, 9)]
    [InlineData(0.87, 9)]
    [InlineData(0.88, 10)]
    [InlineData(1.01, 10)]
    [InlineData(1.02, 11)]
    [InlineData(1.17, 11)]
    [InlineData(1.18, 12)]
    [InlineData(5.00, 12)]  // well above, saturates at 12
    public void Level_returns_expected_Beaufort_value(double windSpeed, int expected)
    {
        Assert.Equal(expected, BeaufortScale.Level(windSpeed));
    }

    [Fact]
    public void Level_treats_negative_input_as_zero()
    {
        Assert.Equal(0, BeaufortScale.Level(-0.5));
    }
}
