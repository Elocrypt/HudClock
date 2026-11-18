using HudClock.Domain.Calendar;
using Xunit;

namespace HudClock.Tests.Domain.Calendar;

public class SeasonWordingTests
{
    [Theory]
    [InlineData("en-US", false)]
    [InlineData("en-CA", false)]
    [InlineData("fr-CA", false)]
    [InlineData("en-GB", true)]
    [InlineData("en-AU", true)]
    [InlineData("de-DE", true)]
    [InlineData("en", false)]   // no region: keep the pre-4.5.0 behavior
    [InlineData("", false)]
    public void Region_decides_when_setting_is_auto(string culture, bool expectAutumn)
    {
        Assert.Equal(expectAutumn, SeasonWording.UseAutumn(null, culture));
    }

    [Theory]
    [InlineData(true, "en-US", true)]
    [InlineData(false, "en-GB", false)]
    public void Explicit_setting_overrides_region(bool setting, string culture, bool expectAutumn)
    {
        Assert.Equal(expectAutumn, SeasonWording.UseAutumn(setting, culture));
    }
}
