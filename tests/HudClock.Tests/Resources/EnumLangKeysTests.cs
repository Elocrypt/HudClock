using HudClock.Configuration;
using HudClock.Resources;

namespace HudClock.Tests.Resources;

/// <summary>
/// Tests cast enum values to <see cref="int"/> at the <c>[InlineData]</c> site so the
/// public test method's parameter list contains only primitives. Passing internal
/// enums directly would violate CS0051 (parameter less accessible than method)
/// regardless of <c>InternalsVisibleTo</c>.
/// </summary>
public class EnumLangKeysTests
{
    [Theory]
    [InlineData((int)HudAnchor.TopLeft,      "topleft")]
    [InlineData((int)HudAnchor.TopCenter,    "topcenter")]
    [InlineData((int)HudAnchor.TopRight,     "topright")]
    [InlineData((int)HudAnchor.BottomLeft,   "bottomleft")]
    [InlineData((int)HudAnchor.BottomCenter, "bottomcenter")]
    [InlineData((int)HudAnchor.BottomRight,  "bottomright")]
    public void HudAnchor_maps_to_suffix(int raw, string expected)
    {
        Assert.Equal(expected, ((HudAnchor)raw).ToKeySuffix());
    }

    [Theory]
    [InlineData((int)TimeFormat.TwelveHour,     "twelvehour")]
    [InlineData((int)TimeFormat.TwentyFourHour, "twentyfourhour")]
    public void TimeFormat_maps_to_suffix(int raw, string expected)
    {
        Assert.Equal(expected, ((TimeFormat)raw).ToKeySuffix());
    }

    [Theory]
    [InlineData((int)WindDisplay.BeaufortText, "beauforttext")]
    [InlineData((int)WindDisplay.Percentage,   "percentage")]
    [InlineData((int)WindDisplay.Hidden,       "hidden")]
    public void WindDisplay_maps_to_suffix(int raw, string expected)
    {
        Assert.Equal(expected, ((WindDisplay)raw).ToKeySuffix());
    }

    [Theory]
    [InlineData((int)StormDisplay.Always,      "always")]
    [InlineData((int)StormDisplay.TriggerOnly, "triggeronly")]
    [InlineData((int)StormDisplay.Hidden,      "hidden")]
    public void StormDisplay_maps_to_suffix(int raw, string expected)
    {
        Assert.Equal(expected, ((StormDisplay)raw).ToKeySuffix());
    }

    [Theory]
    [InlineData((int)RiftDisplay.Always,               "always")]
    [InlineData((int)RiftDisplay.WorldConfigDependent, "worldconfigdependent")]
    [InlineData((int)RiftDisplay.Hidden,               "hidden")]
    public void RiftDisplay_maps_to_suffix(int raw, string expected)
    {
        Assert.Equal(expected, ((RiftDisplay)raw).ToKeySuffix());
    }

    [Theory]
    [InlineData((int)IconTheme.Modern,  "modern")]
    [InlineData((int)IconTheme.Classic, "classic")]
    public void IconTheme_maps_to_suffix(int raw, string expected)
    {
        Assert.Equal(expected, ((IconTheme)raw).ToKeySuffix());
    }
}
