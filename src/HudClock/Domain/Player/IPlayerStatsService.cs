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

    /// <summary>
    /// Categorical "feels like" label provided by an immersive-temperature
    /// mod, read from <c>bodyTemp.apparentTemp</c> (string). Null when no
    /// mod is providing it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defined by the integration contract in <c>docs/integration.md</c>.
    /// HUD Clock localizes the canonical values <c>Comfy</c>, <c>Cold</c>,
    /// <c>Freezing</c>, <c>Warm</c>, and <c>Hot</c>; other strings render
    /// verbatim.
    /// </para>
    /// <para>
    /// <b>Presence of this property's value is HUD Clock's signal that the
    /// bidirectional/immersive body-temperature system is active.</b> When
    /// non-null the viewmodel extends body-temperature display symmetrically
    /// to warm/HOT states above normal; when null it preserves the vanilla
    /// behaviour of only showing cool/FREEZING below normal (vanilla rests
    /// the body temp at normal + 4, which we hide).
    /// </para>
    /// </remarks>
    string? ApparentTemperatureCategory { get; }

    /// <summary>
    /// Numeric apparent ("felt") temperature in Celsius — environment plus
    /// wind/wetness/humidity/sun modifiers — read from
    /// <c>bodyTemp.apparentTempC</c>. Null when no mod is providing it.
    /// </summary>
    /// <remarks>
    /// Always supplied in Celsius regardless of the player's display
    /// preference; the viewmodel converts to Fahrenheit if requested.
    /// </remarks>
    float? ApparentTemperatureCelsius { get; }
}
