namespace HudClock.Presentation.MainHud;

/// <summary>
/// Stable identifier for each main-HUD line. Used both to name the
/// <c>AddDynamicText</c> elements in the view (via <c>nameof(key)</c>) and to
/// key the viewmodel's line-lookup switch.
/// </summary>
/// <remarks>
/// Order here dictates the on-screen stacking order of visible lines; the
/// viewmodel's <c>VisibleLineKeys()</c> method yields in this order and the
/// view reserves bounds in the order they arrive.
/// </remarks>
internal enum MainHudLineKey
{
    SeasonAndTemperature,
    BodyTemperature,
    DateAndTime,
    Realtime,
    Wind,
    Rainfall,
    Intoxication,
    Rift,
    OnlinePlayers,
}
