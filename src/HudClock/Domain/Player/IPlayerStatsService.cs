namespace HudClock.Domain.Player;

/// <summary>
/// Reads live player-entity stats that aren't part of weather, time, or
/// world systems. Getters return null when the underlying
/// <c>WatchedAttribute</c> isn't present, letting each line degrade
/// independently — a missing intoxication attribute (older VS version,
/// mod conflict, creative mode) doesn't affect body temperature and
/// vice versa.
/// </summary>
/// <remarks>
/// Backed by synchronous reads of <c>Player.Entity.WatchedAttributes</c>,
/// which is a dictionary lookup per call. Cheap enough to hit on every
/// HUD tick without needing a polling or caching layer.
/// </remarks>
internal interface IPlayerStatsService
{
    /// <summary>
    /// Current body temperature in Celsius, as read from
    /// <c>bodyTemp.bodytemp</c>. Null when the attribute is missing
    /// (e.g. the body-temperature subsystem is disabled in world config).
    /// </summary>
    float? BodyTemperatureCelsius { get; }

    /// <summary>
    /// Current intoxication in the normalized range [0, 1]. Null when the
    /// attribute is missing. Consumers typically hide the line when the
    /// value is null <i>or</i> zero.
    /// </summary>
    float? Intoxication { get; }
}
