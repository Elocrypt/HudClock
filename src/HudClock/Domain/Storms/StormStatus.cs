namespace HudClock.Domain.Storms;

/// <summary>
/// Immutable snapshot of temporal storm state at a point in time.
/// </summary>
/// <param name="Enabled">
/// True if the world config allows storms at all (<c>temporalStorms != "off"</c>).
/// When false, all other values are meaningless.
/// </param>
/// <param name="NowActive">True if a storm is currently happening.</param>
/// <param name="DaysUntilNext">
/// Days until the next storm begins, or negative if the game reports a past timestamp.
/// Only meaningful when <paramref name="Enabled"/> is true.
/// </param>
internal readonly record struct StormStatus(bool Enabled, bool NowActive, double DaysUntilNext)
{
    /// <summary>Placeholder value returned when the storm system cannot be read.</summary>
    public static readonly StormStatus Unavailable = new(Enabled: false, NowActive: false, DaysUntilNext: 0);
}
