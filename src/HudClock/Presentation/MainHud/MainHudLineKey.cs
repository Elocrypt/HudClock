namespace HudClock.Presentation.MainHud;

/// <summary>
/// Stable identifier for each main-HUD line. Used both to name the
/// <c>AddDynamicText</c> elements in the view (via <c>nameof(key)</c>) and to
/// key the viewmodel's line-lookup switch.
/// </summary>
/// <remarks>
/// <para>
/// Order here dictates the on-screen stacking order of visible lines; the
/// viewmodel's <c>VisibleLineKeys()</c> method yields in this order and the
/// view reserves bounds in the order they arrive.
/// </para>
/// <para>
/// The four lines that mirror the vanilla Environment dialog —
/// <see cref="DateAndTime"/>, <see cref="SeasonAndTemperature"/> (shown as
/// "Outside Temperature"), <see cref="AreaRainfall"/>, and <see cref="Rift"/>
/// — appear in that relative order to match the vanilla panel. HUD Clock's
/// extra lines are grouped around them (real-time/moon near the date, the
/// body/apparent-temperature lines near the temperature, the current-rainfall
/// line next to area rainfall).
/// </para>
/// </remarks>
internal enum MainHudLineKey
{
    DateAndTime,
    Realtime,
    MoonPhase,
    SeasonAndTemperature,
    BodyTemperature,
    ApparentTemperature,
    AreaRainfall,
    Rainfall,
    Wind,
    Intoxication,
    Rift,
    OnlinePlayers,
}
