using HudClock.Configuration;
using HudClock.Domain.Storms;

namespace HudClock.Presentation.StormHud;

/// <summary>
/// What the storm HUD should be doing right now.
/// </summary>
internal enum StormHudAction
{
    /// <summary>Hide the dialog and clear any text.</summary>
    Hidden,

    /// <summary>Show the "storms are disabled in world config" message.</summary>
    ShowDeactivated,

    /// <summary>Show the "no storm in sight" message.</summary>
    ShowFarAway,

    /// <summary>Show the "storm approaching in hh:mm" countdown.</summary>
    ShowApproaching,

    /// <summary>Show the "storm active" alert.</summary>
    ShowActive,
}

/// <summary>
/// Pure decision function that maps (<see cref="StormStatus"/>,
/// <see cref="StormDisplay"/>) to a <see cref="StormHudAction"/>.
/// </summary>
/// <remarks>
/// <para>
/// Extracted into its own class so the policy is fully unit-testable without
/// any VS, GUI, or tick-loop involvement. The view is then trivial: it takes
/// an <see cref="StormHudAction"/> and renders it.
/// </para>
/// <para>
/// Fixes a 3.x bug where <see cref="StormDisplay.TriggerOnly"/> would
/// auto-close the dialog during active storms. The fix is to treat both
/// <see cref="StormDisplay.Always"/> and <see cref="StormDisplay.TriggerOnly"/>
/// as "visible during an active storm" — the original code only did so for
/// <c>ALWAYS</c>.
/// </para>
/// </remarks>
internal static class StormHudStateMachine
{
    /// <summary>Threshold for "approaching": less than this many days until the next storm.</summary>
    public const double ApproachingThresholdDays = 0.35;

    /// <summary>Compute the action the view should take.</summary>
    public static StormHudAction Decide(StormStatus status, StormDisplay displayMode)
    {
        // User preference: never show, regardless of state.
        if (displayMode == StormDisplay.Hidden) return StormHudAction.Hidden;

        // World config: storms off. Only the ALWAYS mode surfaces this state.
        if (!status.Enabled)
        {
            return displayMode == StormDisplay.Always
                ? StormHudAction.ShowDeactivated
                : StormHudAction.Hidden;
        }

        // Active storm: BOTH Always and TriggerOnly show it. This is the 3.x bug fix.
        if (status.NowActive)
        {
            return StormHudAction.ShowActive;
        }

        // Approaching storm: BOTH Always and TriggerOnly show a countdown.
        bool approaching = status.DaysUntilNext >= 0.0 && status.DaysUntilNext < ApproachingThresholdDays;
        if (approaching)
        {
            return StormHudAction.ShowApproaching;
        }

        // Storms on, nothing imminent: only Always shows the idle "no storm" message.
        return displayMode == StormDisplay.Always
            ? StormHudAction.ShowFarAway
            : StormHudAction.Hidden;
    }
}
