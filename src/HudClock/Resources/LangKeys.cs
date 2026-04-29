namespace HudClock.Resources;

/// <summary>
/// Lang keys owned by this mod, as named constants. Grouped by the UI concern
/// each key serves. Keys are short and deliberately independent of the C#
/// enum names they decorate, so a future enum rename doesn't break
/// translations.
/// </summary>
internal static class LangKeys
{
    // Common labels.
    public const string AmSuffix = "hudclock:am";
    public const string PmSuffix = "hudclock:pm";

    /// <summary>Main HUD lines and formatting strings.</summary>
    public static class Hud
    {
        /// <summary>Format: day-of-month, month-name, year. Example: <c>"{0}. {1}, Year {2}"</c>.</summary>
        public const string Date           = "hudclock:date";
        public const string RealtimePrefix = "hudclock:realtime-prefix";

        /// <summary>Format: "{0} °C".</summary>
        public const string TemperatureCelsius    = "hudclock:temperature-celsius";
        /// <summary>Format: "{0} °F".</summary>
        public const string TemperatureFahrenheit = "hudclock:temperature-fahrenheit";

        /// <summary>Format: "Wind: {0}".</summary>
        public const string WindPrefix       = "hudclock:wind-prefix";
        public const string WindBeaufortStem = "hudclock:wind-beaufort-"; // + 0..12

        /// <summary>
        /// Format: "Body: {0} ({1} {2})" where {0} is a localized state
        /// label (cool/freezing/warm/hot), {1} is a signed delta from
        /// normal (e.g. "-2.4"), and {2} is the unit (°C / °F).
        /// </summary>
        public const string BodyTemperaturePrefix = "hudclock:body-temperature-prefix";

        /// <summary>State label: below normal but not yet taking damage.</summary>
        public const string BodyTempStateCool = "hudclock:body-temperature-state-cool";

        /// <summary>State label: at or below freezing-damage threshold.</summary>
        public const string BodyTempStateFreezing = "hudclock:body-temperature-state-freezing";

        /// <summary>
        /// State label: above normal but not yet taking heatstroke damage.
        /// Only reachable when an immersive-temperature mod is active —
        /// vanilla rests body temp at <c>normal + 4</c> which we hide.
        /// </summary>
        public const string BodyTempStateWarm = "hudclock:body-temperature-state-warm";

        /// <summary>
        /// State label: at or above the heatstroke-damage threshold
        /// (<c>normal + 4</c>). Mirrors <see cref="BodyTempStateFreezing"/>.
        /// Only reachable with an immersive-temperature mod.
        /// </summary>
        public const string BodyTempStateHot = "hudclock:body-temperature-state-hot";

        /// <summary>
        /// Format: "Apparent: {0} ({1})" where {0} is a temperature string
        /// (already unit-formatted) and {1} is a localized "feels like"
        /// state label. Used for the apparent-temperature line provided
        /// by an immersive-temperature mod.
        /// </summary>
        public const string ApparentTemperaturePrefix = "hudclock:apparent-temperature-prefix";

        /// <summary>
        /// Format: "Apparent: {0}" — used when the mod publishes a numeric
        /// apparent temperature but no categorical state. Falls back to the
        /// suffixed prefix when both are present.
        /// </summary>
        public const string ApparentTemperaturePrefixUncategorized = "hudclock:apparent-temperature-prefix-uncategorized";

        /// <summary>
        /// Stem for apparent-temperature category labels. The suffix is the
        /// lowercased category string supplied by the integrating mod —
        /// canonical values are <c>comfy</c>, <c>cold</c>, <c>freezing</c>,
        /// <c>warm</c>, <c>hot</c>.
        /// </summary>
        public const string ApparentTempStateStem = "hudclock:apparent-temperature-state-";

        /// <summary>Format: "Intoxication: {0}%" — {0} is an integer 1..100.</summary>
        public const string Intoxication = "hudclock:intoxication";

        /// <summary>Format: "Rainfall: {0}" where {0} is a localized bucket label.</summary>
        public const string RainfallPrefix = "hudclock:rainfall-prefix";

        /// <summary>Stem for rainfall buckets: + "rare" / "light" / "moderate" / "high" / "veryhigh".</summary>
        public const string RainfallStem = "hudclock:rainfall-";

        /// <summary>Format: "Players online: {0}".</summary>
        public const string OnlinePlayers = "hudclock:online-players";

        public const string SeasonStem = "hudclock:season-"; // + winter / spring / summer / fall

        public const string MonthStem  = "hudclock:month-";  // + january..december
    }

    /// <summary>Temporal storm HUD messages.</summary>
    public static class Storm
    {
        public const string FarAway     = "hudclock:storm-far-away";
        public const string Deactivated = "hudclock:storm-deactivated";
        /// <summary>Format: "Storm approaches {0}".</summary>
        public const string Approaching = "hudclock:storm-approaching";
        public const string Active      = "hudclock:storm-active";
    }

    /// <summary>Claim banner.</summary>
    public static class Claim
    {
        /// <summary>Format: "Claim of {0}".</summary>
        public const string Owner = "hudclock:claim-owner";
    }

    /// <summary>Hotkey labels shown in the game's Controls settings screen.</summary>
    public static class Keybind
    {
        public const string SettingsDialog = "hudclock:keybind-settings-dialog";
        public const string MainHud        = "hudclock:keybind-main-hud";
        public const string StormHud       = "hudclock:keybind-storm-hud";
    }

    /// <summary>Mod settings dialog labels and option choices.</summary>
    public static class Settings
    {
        public const string Title = "hudclock:settings-title";

        // Section headers (new in 4.0 for the redesigned layout).
        public const string SectionDisplay     = "hudclock:settings-section-display";
        public const string SectionAppearance  = "hudclock:settings-section-appearance";
        public const string SectionTime        = "hudclock:settings-section-time";
        public const string SectionWeather     = "hudclock:settings-section-weather";
        public const string SectionPlayerStats = "hudclock:settings-section-player-stats";
        public const string SectionEvents      = "hudclock:settings-section-events";
        public const string SectionMultiplayer = "hudclock:settings-section-multiplayer";

        // Switch labels.
        public const string ShowSeason             = "hudclock:settings-show-season";
        public const string ShowTemperature        = "hudclock:settings-show-temperature";
        public const string Fahrenheit             = "hudclock:settings-fahrenheit";
        public const string ShowRainfall           = "hudclock:settings-show-rainfall";
        public const string ShowDate               = "hudclock:settings-show-date";
        public const string ShowTime               = "hudclock:settings-show-time";
        public const string ShowRealtime           = "hudclock:settings-show-realtime";
        public const string ShowRoom               = "hudclock:settings-show-room";
        public const string ShowBodyTemperature    = "hudclock:settings-show-body-temperature";
        public const string ShowIntoxication       = "hudclock:settings-show-intoxication";
        public const string ShowApparentTemperature = "hudclock:settings-show-apparent-temperature";
        public const string ShowOnlinePlayers      = "hudclock:settings-show-online-players";
        public const string ShowClaim              = "hudclock:settings-show-claim";

        // Dropdown labels.
        public const string HudPosition = "hudclock:settings-hud-position";
        public const string IconTheme   = "hudclock:settings-icon-theme";
        public const string TimeFormat  = "hudclock:settings-time-format";
        public const string Wind        = "hudclock:settings-wind";
        public const string StormDialog = "hudclock:settings-storm";
        public const string StormAnchor = "hudclock:settings-storm-anchor";
        public const string RiftDialog  = "hudclock:settings-rift";

        // Dropdown option stems; the viewmodel appends a lowercase enum-derived suffix
        // from EnumLangKeys, which is why these look incomplete on their own.
        public const string HudPositionStem = "hudclock:settings-hud-position-";
        public const string IconThemeStem   = "hudclock:settings-icon-theme-";
        public const string TimeFormatStem  = "hudclock:settings-time-format-";
        public const string WindStem        = "hudclock:settings-wind-";
        public const string StormStem       = "hudclock:settings-storm-";
        public const string RiftStem        = "hudclock:settings-rift-";
    }
}
