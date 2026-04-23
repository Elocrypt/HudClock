using HudClock.Configuration;
using HudClock.Domain.Storms;
using HudClock.Presentation.StormHud;

namespace HudClock.Tests.Presentation.StormHud;

public class StormHudStateMachineTests
{
    // Synthetic status values for use in [Theory] arguments.
    private static StormStatus Disabled() => new(Enabled: false, NowActive: false, DaysUntilNext: 0);
    private static StormStatus FarAway() => new(Enabled: true, NowActive: false, DaysUntilNext: 2.0);
    private static StormStatus Approaching() => new(Enabled: true, NowActive: false, DaysUntilNext: 0.2);
    private static StormStatus JustAtThreshold() => new(Enabled: true, NowActive: false, DaysUntilNext: StormHudStateMachine.ApproachingThresholdDays);
    private static StormStatus JustUnderThreshold() => new(Enabled: true, NowActive: false, DaysUntilNext: StormHudStateMachine.ApproachingThresholdDays - 0.0001);
    private static StormStatus Active() => new(Enabled: true, NowActive: true, DaysUntilNext: 0);

    // --- Hidden mode: user said no, always respect that ---

    [Fact]
    public void Hidden_mode_never_shows_regardless_of_state()
    {
        Assert.Equal(StormHudAction.Hidden, StormHudStateMachine.Decide(Disabled(), StormDisplay.Hidden));
        Assert.Equal(StormHudAction.Hidden, StormHudStateMachine.Decide(FarAway(), StormDisplay.Hidden));
        Assert.Equal(StormHudAction.Hidden, StormHudStateMachine.Decide(Approaching(), StormDisplay.Hidden));
        Assert.Equal(StormHudAction.Hidden, StormHudStateMachine.Decide(Active(), StormDisplay.Hidden));
    }

    // --- Always mode: surfaces every state ---

    [Fact]
    public void Always_mode_surfaces_deactivated_state()
    {
        Assert.Equal(StormHudAction.ShowDeactivated, StormHudStateMachine.Decide(Disabled(), StormDisplay.Always));
    }

    [Fact]
    public void Always_mode_surfaces_far_away_state()
    {
        Assert.Equal(StormHudAction.ShowFarAway, StormHudStateMachine.Decide(FarAway(), StormDisplay.Always));
    }

    [Fact]
    public void Always_mode_surfaces_approaching_state()
    {
        Assert.Equal(StormHudAction.ShowApproaching, StormHudStateMachine.Decide(Approaching(), StormDisplay.Always));
    }

    [Fact]
    public void Always_mode_surfaces_active_state()
    {
        Assert.Equal(StormHudAction.ShowActive, StormHudStateMachine.Decide(Active(), StormDisplay.Always));
    }

    // --- TriggerOnly mode: only surfaces "imminent" states ---

    [Fact]
    public void TriggerOnly_hides_deactivated_state()
    {
        Assert.Equal(StormHudAction.Hidden, StormHudStateMachine.Decide(Disabled(), StormDisplay.TriggerOnly));
    }

    [Fact]
    public void TriggerOnly_hides_far_away_state()
    {
        Assert.Equal(StormHudAction.Hidden, StormHudStateMachine.Decide(FarAway(), StormDisplay.TriggerOnly));
    }

    [Fact]
    public void TriggerOnly_surfaces_approaching_state()
    {
        Assert.Equal(StormHudAction.ShowApproaching, StormHudStateMachine.Decide(Approaching(), StormDisplay.TriggerOnly));
    }

    /// <summary>
    /// Regression test for the 3.x bug where TriggerOnly auto-closed the dialog
    /// during active storms — exactly the opposite of the user's intent.
    /// </summary>
    [Fact]
    public void TriggerOnly_shows_active_state()
    {
        Assert.Equal(StormHudAction.ShowActive, StormHudStateMachine.Decide(Active(), StormDisplay.TriggerOnly));
    }

    // --- Threshold edge cases ---

    [Fact]
    public void Threshold_boundary_is_not_approaching()
    {
        // 0.35 days exactly falls outside the "approaching" window.
        Assert.Equal(StormHudAction.ShowFarAway,
            StormHudStateMachine.Decide(JustAtThreshold(), StormDisplay.Always));
    }

    [Fact]
    public void Just_under_threshold_is_approaching()
    {
        Assert.Equal(StormHudAction.ShowApproaching,
            StormHudStateMachine.Decide(JustUnderThreshold(), StormDisplay.Always));
    }

    [Fact]
    public void Negative_days_until_next_is_not_approaching()
    {
        // Corrupt/stale data shouldn't be rendered as "approaching".
        var stale = new StormStatus(Enabled: true, NowActive: false, DaysUntilNext: -0.1);
        Assert.Equal(StormHudAction.ShowFarAway,
            StormHudStateMachine.Decide(stale, StormDisplay.Always));
    }
}
