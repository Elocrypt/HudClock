namespace HudClock.Domain.Weather;

/// <summary>
/// Maps a world-generation ("area") rainfall value in [0, 1] to the suffix of
/// the vanilla <c>freq-*</c> lang key the Environment dialog uses for its
/// "Rainfall in this Area" line.
/// </summary>
/// <remarks>
/// Thresholds taken from the vanilla value→word table documented on the
/// Vintage Story wiki (Weather page). The actual binning lives in the
/// closed-source character dialog, so these boundaries are replicated rather
/// than read from the game; they were last verified against a 2023 wiki
/// revision and should be spot-checked in-game on a major VS update.
/// Returning the <c>freq-*</c> suffix lets the viewmodel reuse the game's own
/// (already-localized) descriptor strings instead of shipping our own.
/// </remarks>
internal static class RainfallFrequency
{
    /// <summary>
    /// Lang-key suffix for the given area rainfall. Combine with the
    /// <c>"freq-"</c> stem to get the vanilla key (e.g. <c>"freq-common"</c>).
    /// </summary>
    public static string KeySuffix(float rainfall) => rainfall switch
    {
        < 0.15f => "veryrare",
        < 0.30f => "rarely",
        < 0.45f => "uncommon",
        < 0.70f => "common",
        < 0.90f => "verycommon",
        _ => "allthetime",
    };
}
