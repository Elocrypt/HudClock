namespace HudClock.Presentation.MainHud;

/// <summary>
/// Stable identifier for each main-HUD line. Used both to name the
/// <c>AddDynamicText</c> elements in the view (via <c>nameof(key)</c>) and to
/// key the viewmodel's line-lookup switch.
/// </summary>
internal enum MainHudLineKey
{
    SeasonAndTemperature,
    DateAndTime,
    Realtime,
    Wind,
    Rift,
    OnlinePlayers,
}
