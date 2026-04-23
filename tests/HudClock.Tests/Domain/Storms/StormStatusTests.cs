using HudClock.Domain.Storms;

namespace HudClock.Tests.Domain.Storms;

public class StormStatusTests
{
    [Fact]
    public void Unavailable_is_not_enabled_and_not_active()
    {
        Assert.False(StormStatus.Unavailable.Enabled);
        Assert.False(StormStatus.Unavailable.NowActive);
        Assert.Equal(0, StormStatus.Unavailable.DaysUntilNext);
    }

    [Fact]
    public void Values_with_same_content_compare_equal()
    {
        var a = new StormStatus(Enabled: true, NowActive: false, DaysUntilNext: 1.5);
        var b = new StormStatus(Enabled: true, NowActive: false, DaysUntilNext: 1.5);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Values_with_different_content_compare_unequal()
    {
        var a = new StormStatus(Enabled: true, NowActive: false, DaysUntilNext: 1.5);
        var b = new StormStatus(Enabled: true, NowActive: true, DaysUntilNext: 0.0);

        Assert.NotEqual(a, b);
    }
}
