using System;
using System.Collections.Generic;
using System.Globalization;
using HudClock.Configuration;
using HudClock.Domain.Calendar;
using HudClock.Domain.Claims;
using HudClock.Domain.Player;
using HudClock.Domain.Rifts;
using HudClock.Domain.Rooms;
using HudClock.Domain.Time;
using HudClock.Domain.Weather;
using HudClock.Resources;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HudClock.Presentation.MainHud;

/// <summary>
/// Pure viewmodel for the main HUD. Queries domain services and produces
/// strongly-typed display strings; the view renders these without applying
/// any additional logic.
/// </summary>
/// <remarks>
/// <para>
/// Organized as a two-phase workflow: <see cref="Tick"/> pulls fresh data from
/// services into backing fields, and individual string properties expose the
/// current line content. When a line is suppressed by settings it returns null;
/// the view treats null as "don't render this line".
/// </para>
/// <para>
/// Deliberately holds no VS API references beyond <see cref="BlockPos"/> and
/// <see cref="EnumSeason"/>, and never touches the GUI composer. That boundary
/// makes the whole class unit-testable against hand-rolled service fakes.
/// </para>
/// </remarks>
internal sealed class MainHudViewModel
{
    /// <summary>Delegate that translates a lang key to its localized value.</summary>
    public delegate string Translator(string key);

    /// <summary>Delegate that translates a lang key with format arguments.</summary>
    public delegate string FormatTranslator(string key, params object[] args);

    /// <summary>Delegate that returns the current block position to query.</summary>
    public delegate BlockPos PlayerPositionProvider();

    /// <summary>Delegate that returns the current online player count.</summary>
    public delegate int OnlinePlayerCountProvider();

    private readonly HudClockSettings _settings;
    private readonly ITimeFormatter _timeFormatter;
    private readonly ICalendarService _calendar;
    private readonly IWeatherService _weather;
    private readonly IRiftService _rift;
    private readonly IRoomService _room;
    private readonly IClaimService _claim;
    private readonly IPlayerStatsService _playerStats;
    private readonly Translator _translate;
    private readonly FormatTranslator _translateFormat;
    private readonly PlayerPositionProvider _playerPosition;
    private readonly OnlinePlayerCountProvider _onlinePlayerCount;
    private readonly bool _isMultiplayer;

    // Cached tick results. Written by Tick, read by the string properties.
    private CalendarSnapshot _calendarSnapshot;
    private EnumSeason _season = EnumSeason.Winter;
    private float _temperatureCelsius;
    private double _windSpeed;
    private string? _riftCode;
    private RoomStatus _roomStatus = RoomStatus.None;
    private ClaimInfo? _claim_cache;
    private int _onlinePlayers;
    private float? _bodyTemperatureCelsius;
    private float? _intoxication;
    private float _rainfall;

    // Snapshot of what was baked into the current layout's static icons.
    // Static custom-draws rasterize once per Compose, so the view has no way
    // to re-paint an icon when the underlying state changes. We compare these
    // snapshots against the current tick's values in HasVisibleIconChanged;
    // the controller uses that flag to decide whether to call Rebuild on the
    // view. Initial sentinel values that cannot match any real first-tick
    // result guarantee the first Tick() reports a change — that way even an
    // unchanged initial load correctly bakes the current season/room into
    // the composer.
    private EnumSeason? _lastBakedSeason;
    private RoomStatus? _lastBakedRoomStatus;
    // Also track the theme baked into the composer — a theme change has to
    // force a Rebuild so the new art gets rasterized into the static-draw
    // cache. Without this, toggling Modern/Classic in settings wouldn't
    // take effect until the next season or room transition.
    private IconTheme? _lastBakedTheme;
    // Bitmask of MainHudLineKey values that were laid out by the last
    // Rebuild. Some lines (intoxication) appear/disappear without any
    // settings change — when that happens, the dynamic-text element doesn't
    // exist in the composer yet, so a fast-path UpdateTexts has nothing to
    // call SetNewText on. Detecting a set change here promotes the tick to
    // a full Rebuild.
    private long _lastBakedLineSetMask;

    public MainHudViewModel(
        HudClockSettings settings,
        ITimeFormatter timeFormatter,
        ICalendarService calendar,
        IWeatherService weather,
        IRiftService rift,
        IRoomService room,
        IClaimService claim,
        IPlayerStatsService playerStats,
        Translator translate,
        FormatTranslator translateFormat,
        PlayerPositionProvider playerPosition,
        OnlinePlayerCountProvider onlinePlayerCount,
        bool isMultiplayer)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeFormatter = timeFormatter ?? throw new ArgumentNullException(nameof(timeFormatter));
        _calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        _weather = weather ?? throw new ArgumentNullException(nameof(weather));
        _rift = rift ?? throw new ArgumentNullException(nameof(rift));
        _room = room ?? throw new ArgumentNullException(nameof(room));
        _claim = claim ?? throw new ArgumentNullException(nameof(claim));
        _playerStats = playerStats ?? throw new ArgumentNullException(nameof(playerStats));
        _translate = translate ?? throw new ArgumentNullException(nameof(translate));
        _translateFormat = translateFormat ?? throw new ArgumentNullException(nameof(translateFormat));
        _playerPosition = playerPosition ?? throw new ArgumentNullException(nameof(playerPosition));
        _onlinePlayerCount = onlinePlayerCount ?? throw new ArgumentNullException(nameof(onlinePlayerCount));
        _isMultiplayer = isMultiplayer;
    }

    /// <summary>
    /// Pull fresh data from all domain services. Call at the HUD's tick rate
    /// (default 2.5s). The string properties below reflect the result.
    /// </summary>
    public void Tick()
    {
        BlockPos pos = _playerPosition();

        _calendarSnapshot = _calendar.GetSnapshot();
        _season = _calendar.GetSeason(pos);
        _temperatureCelsius = _weather.GetTemperatureCelsius(pos);
        _windSpeed = _weather.GetWindSpeed(pos);
        // Query the rift code whenever policy might show it; the service returns null
        // if the underlying mod system actually failed to load. We don't gate on
        // IsAvailable here because RiftDisplay.Always means "show regardless of
        // world-config availability".
        _riftCode = ShouldShowRift() ? _rift.GetCurrentActivityCode() : null;
        _roomStatus = _room.CurrentStatus;
        _claim_cache = _settings.Claim.ShowClaimedArea ? _claim.GetClaimAt(pos) : null;
        _onlinePlayers = _isMultiplayer ? _onlinePlayerCount() : 0;

        // Player-stat reads are gated by the same settings that decide line
        // visibility. The service's one-shot "missing attribute" warning then
        // only fires for users who actually asked to see the line — avoids
        // noise for players who don't use these features.
        _bodyTemperatureCelsius = _settings.PlayerStats.ShowBodyTemperature ? _playerStats.BodyTemperatureCelsius : null;
        _intoxication = _settings.PlayerStats.ShowIntoxication ? _playerStats.Intoxication : null;
        _rainfall = _settings.Weather.ShowRainfall ? _weather.GetRainfall(pos) : 0f;
    }

    /// <summary>
    /// True when something has changed since the last Rebuild that requires
    /// a full re-compose rather than the fast text-only update path. Triggers:
    /// season or room icon state, active icon theme, OR the set of currently
    /// visible lines (e.g. an intox line appearing for the first time when
    /// the player drinks).
    /// </summary>
    /// <remarks>
    /// VS's <c>AddStaticCustomDraw</c> bakes the bitmap into the composer's
    /// cached texture at Compose time, so an icon swap needs a Rebuild.
    /// Lines that didn't exist at the previous Compose time also need a
    /// Rebuild — the dynamic-text element isn't in the composer yet, so
    /// <c>UpdateTexts</c> would have nothing to update.
    /// </remarks>
    public bool HasVisibleIconChanged =>
        _lastBakedSeason != _season
        || _lastBakedRoomStatus != _roomStatus
        || _lastBakedTheme != _settings.Appearance.IconTheme
        || _lastBakedLineSetMask != ComputeVisibleLineSetMask();

    /// <summary>
    /// Called by the view after each successful Rebuild to snapshot the
    /// state baked into the current composition. Resets
    /// <see cref="HasVisibleIconChanged"/> until the next change.
    /// </summary>
    public void MarkIconsBaked()
    {
        _lastBakedSeason = _season;
        _lastBakedRoomStatus = _roomStatus;
        _lastBakedTheme = _settings.Appearance.IconTheme;
        _lastBakedLineSetMask = ComputeVisibleLineSetMask();
    }

    /// <summary>
    /// Compact bitmask of currently-visible <see cref="MainHudLineKey"/>
    /// values, used to detect set changes between ticks without allocating.
    /// </summary>
    private long ComputeVisibleLineSetMask()
    {
        long mask = 0;
        if (IsSeasonAndTemperatureLineVisible) mask |= 1L << (int)MainHudLineKey.SeasonAndTemperature;
        if (IsBodyTemperatureLineVisible)      mask |= 1L << (int)MainHudLineKey.BodyTemperature;
        if (IsDateAndTimeLineVisible)          mask |= 1L << (int)MainHudLineKey.DateAndTime;
        if (IsRealtimeLineVisible)             mask |= 1L << (int)MainHudLineKey.Realtime;
        if (IsWindLineVisible)                 mask |= 1L << (int)MainHudLineKey.Wind;
        if (IsRainfallLineVisible)             mask |= 1L << (int)MainHudLineKey.Rainfall;
        if (IsIntoxicationLineVisible)         mask |= 1L << (int)MainHudLineKey.Intoxication;
        if (IsRiftLineVisible)                 mask |= 1L << (int)MainHudLineKey.Rift;
        if (IsOnlinePlayersLineVisible)        mask |= 1L << (int)MainHudLineKey.OnlinePlayers;
        return mask;
    }

    // --- Visibility helpers used by the view to decide which lines to lay out. ---

    public bool IsSeasonAndTemperatureLineVisible =>
        _settings.Weather.ShowSeason || _settings.Weather.ShowTemperature;

    /// <summary>
    /// Body-temperature line is visible only when the player is at or
    /// below normal body temperature (37 °C raw). Comfortable / warm
    /// states hide the line — the player doesn't need a number when
    /// they're fine. Matches Status HUD Continued's body-heat behaviour.
    /// </summary>
    public bool IsBodyTemperatureLineVisible =>
        _settings.PlayerStats.ShowBodyTemperature
        && _bodyTemperatureCelsius.HasValue
        && _bodyTemperatureCelsius.Value < BodyTempNormal;

    /// <summary>True when body temperature has crossed the freezing-damage threshold.</summary>
    /// <remarks>
    /// Mirrors <c>EntityBehaviorBodyTemperature</c>: damage starts when
    /// <c>NormalBodyTemperature - CurBodyTemperature > 4</c>, i.e. when
    /// the raw value drops below 33 °C.
    /// </remarks>
    public bool IsFreezing =>
        _bodyTemperatureCelsius.HasValue
        && _bodyTemperatureCelsius.Value <= BodyTempFreezingThreshold;

    // Vanilla survival constants from EntityBehaviorBodyTemperature.cs.
    // Hard-coded rather than configurable: matching the source guarantees
    // our threshold tracks the game's actual damage logic.
    private const float BodyTempNormal = 37f;
    private const float BodyTempFreezingThreshold = 33f;

    public bool IsDateAndTimeLineVisible =>
        _settings.Time.ShowDate || _settings.Time.ShowTime;

    public bool IsRealtimeLineVisible => _settings.Time.ShowRealtime;

    public bool IsWindLineVisible => _settings.Weather.Wind != WindDisplay.Hidden;

    public bool IsRainfallLineVisible => _settings.Weather.ShowRainfall;

    /// <summary>
    /// Intoxication line is visible only when the setting is on <i>and</i> the
    /// player is actually intoxicated. Hidden at zero matches Status HUD
    /// Continued's UX — the line would otherwise show "0%" for most of a
    /// playthrough.
    /// </summary>
    public bool IsIntoxicationLineVisible =>
        _settings.PlayerStats.ShowIntoxication
        && _intoxication.HasValue
        && _intoxication.Value > 0f;

    public bool IsRiftLineVisible =>
        ShouldShowRift() && _riftCode is not null;

    public bool IsOnlinePlayersLineVisible =>
        _settings.Multiplayer.ShowOnlinePlayerCount && _isMultiplayer;

    public bool IsRoomIndicatorVisible =>
        _settings.Room.ShowRoomIndicator && _roomStatus != RoomStatus.None;

    /// <summary>True when the HUD has no content to render at all.</summary>
    public bool IsEmpty =>
        !IsSeasonAndTemperatureLineVisible
        && !IsBodyTemperatureLineVisible
        && !IsDateAndTimeLineVisible
        && !IsRealtimeLineVisible
        && !IsWindLineVisible
        && !IsRainfallLineVisible
        && !IsIntoxicationLineVisible
        && !IsRiftLineVisible
        && !IsOnlinePlayersLineVisible
        && !IsRoomIndicatorVisible;

    // --- Line content. Null when the corresponding line is hidden. ---

    /// <summary>Formatted season and/or temperature line, or null when hidden.</summary>
    public string? SeasonAndTemperatureText
    {
        get
        {
            bool showSeason = _settings.Weather.ShowSeason;
            bool showTemp = _settings.Weather.ShowTemperature;
            if (!showSeason && !showTemp) return null;

            if (showSeason && showTemp)
                return $"{SeasonName()}, {TemperatureString(_temperatureCelsius)}";
            if (showSeason)
                return SeasonName();
            return TemperatureString(_temperatureCelsius);
        }
    }

    /// <summary>
    /// Formatted body-temperature line, or null when hidden. Shows a
    /// comfort signal — "cool" or "freezing" — with the temperature
    /// deviation from normal (37 °C raw) in the player's chosen unit.
    /// The HUD never shows an absolute body temperature: the raw
    /// watched-attribute value differs from the character GUI by design,
    /// and re-deriving the GUI's display value would require duplicating
    /// the survival mod's full clothing/wetness/climate computation.
    /// </summary>
    public string? BodyTemperatureText
    {
        get
        {
            if (!IsBodyTemperatureLineVisible) return null;

            // Deviation is always negative or zero when the line is
            // visible (visibility check excludes raw >= 37). Display as
            // a signed temperature delta so "-2.4 °C" reads as "two and
            // a bit degrees colder than normal".
            float deviation = _bodyTemperatureCelsius!.Value - BodyTempNormal;

            // For Fahrenheit users, convert the *delta* — not via the
            // C->F formula (which would add 32) but as a pure scale
            // factor. A delta of 1 °C is a delta of 1.8 °F.
            float displayDelta = _settings.Weather.Fahrenheit ? deviation * 9f / 5f : deviation;
            string deltaStr = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0:+0.0;-0.0;0.0}", displayDelta);
            string unit = _settings.Weather.Fahrenheit ? "°F" : "°C";

            string stateLabel = _translate(IsFreezing
                ? LangKeys.Hud.BodyTempStateFreezing
                : LangKeys.Hud.BodyTempStateCool);

            return _translateFormat(LangKeys.Hud.BodyTemperaturePrefix, stateLabel, deltaStr, unit);
        }
    }

    /// <summary>
    /// Formatted intoxication line (e.g. "Intoxication: 45%"), or null when
    /// the player is sober or the line is disabled. The WatchedAttribute is
    /// in [0, 1]; we render as whole-number percent.
    /// </summary>
    public string? IntoxicationText
    {
        get
        {
            if (!IsIntoxicationLineVisible) return null;
            int percent = (int)System.Math.Round(_intoxication!.Value * 100f);
            return _translateFormat(LangKeys.Hud.Intoxication, percent);
        }
    }

    /// <summary>
    /// Formatted rainfall line (e.g. "Rainfall: Moderate"), or null when
    /// the line is disabled. Maps the normalized [0, 1] rainfall value to
    /// the same discrete labels the vanilla Environment dialog uses.
    /// </summary>
    public string? RainfallText
    {
        get
        {
            if (!IsRainfallLineVisible) return null;
            string label = _translate(LangKeys.Hud.RainfallStem + RainfallBucket(_rainfall));
            return _translateFormat(LangKeys.Hud.RainfallPrefix, label);
        }
    }

    /// <summary>
    /// Map a normalized rainfall value to a vanilla-matching descriptor
    /// suffix. Thresholds chosen to match the Environment dialog's "Rare /
    /// Light / Moderate / High / Very high" buckets.
    /// </summary>
    private static string RainfallBucket(float r)
    {
        if (r < 0.10f) return "rare";
        if (r < 0.30f) return "light";
        if (r < 0.55f) return "moderate";
        if (r < 0.80f) return "high";
        return "veryhigh";
    }

    /// <summary>Formatted date and/or time line, or null when hidden.</summary>
    public string? DateAndTimeText
    {
        get
        {
            bool showDate = _settings.Time.ShowDate;
            bool showTime = _settings.Time.ShowTime;
            if (!showDate && !showTime) return null;

            string date = _translateFormat(
                LangKeys.Hud.Date,
                _calendarSnapshot.DayOfMonth,
                _translate(LangKeys.Hud.MonthStem + _calendarSnapshot.MonthKey),
                _calendarSnapshot.Year);

            string time = _timeFormatter.Format(_calendarSnapshot.Hour, _calendarSnapshot.Minute);

            if (showDate && showTime) return $"{date} {time}";
            if (showDate) return date;
            return time;
        }
    }

    /// <summary>Formatted real-world time line, or null when hidden.</summary>
    public string? RealtimeText
    {
        get
        {
            if (!_settings.Time.ShowRealtime) return null;
            DateTime now = DateTime.Now;
            string prefix = _translate(LangKeys.Hud.RealtimePrefix);
            string time = _timeFormatter.Format(now.Hour, now.Minute);
            return $"{prefix}{time}";
        }
    }

    /// <summary>Formatted wind speed line, or null when hidden.</summary>
    public string? WindText
    {
        get
        {
            switch (_settings.Weather.Wind)
            {
                case WindDisplay.Hidden:
                    return null;

                case WindDisplay.Percentage:
                {
                    string percent = Math.Round(_windSpeed * 100.0, 0).ToString(CultureInfo.InvariantCulture) + "%";
                    return _translateFormat(LangKeys.Hud.WindPrefix, percent);
                }

                case WindDisplay.BeaufortText:
                {
                    int level = BeaufortScale.Level(_windSpeed);
                    string label = _translate(LangKeys.Hud.WindBeaufortStem + level);
                    return _translateFormat(LangKeys.Hud.WindPrefix, label);
                }

                default:
                    return null;
            }
        }
    }

    /// <summary>Rift-activity line, or null when hidden.</summary>
    public string? RiftText
    {
        get
        {
            if (!IsRiftLineVisible) return null;
            // 3.x used "rift-activity-{code}" as the lang key; carrying that convention forward
            // lets VS's own lang contributions continue to work without remapping.
            string label = _translate("rift-activity-" + _riftCode);
            return _translateFormat("Rift activity: {0}", label);
        }
    }

    /// <summary>Online-players line, or null when hidden.</summary>
    public string? OnlinePlayersText
    {
        get
        {
            if (!IsOnlinePlayersLineVisible) return null;
            return _translateFormat(LangKeys.Hud.OnlinePlayers, _onlinePlayers);
        }
    }

    /// <summary>Season icon key (for the large background image in the HUD).</summary>
    public string? SeasonIconPath => _season switch
    {
        EnumSeason.Spring => AssetPaths.Hud.SeasonSpring(_settings.Appearance.IconTheme),
        EnumSeason.Summer => AssetPaths.Hud.SeasonSummer(_settings.Appearance.IconTheme),
        EnumSeason.Fall   => AssetPaths.Hud.SeasonFall(_settings.Appearance.IconTheme),
        EnumSeason.Winter => AssetPaths.Hud.SeasonWinter(_settings.Appearance.IconTheme),
        _ => null,
    };

    /// <summary>Room-indicator icon path, or null when no indicator is active.</summary>
    public string? RoomIconPath
    {
        get
        {
            if (!_settings.Room.ShowRoomIndicator) return null;
            IconTheme theme = _settings.Appearance.IconTheme;
            return _roomStatus switch
            {
                RoomStatus.Greenhouse => AssetPaths.Room.Greenhouse(theme),
                RoomStatus.SmallRoom  => AssetPaths.Room.Cellar(theme),
                RoomStatus.Room       => AssetPaths.Room.Generic(theme),
                _ => null,
            };
        }
    }

    /// <summary>Enumerate the active (non-null) text lines in stacking order.</summary>
    /// <remarks>
    /// The view uses this to know how many dynamic text elements to create
    /// during <c>Rebuild()</c>. Order here matches rendering order.
    /// </remarks>
    public IEnumerable<MainHudLineKey> VisibleLineKeys()
    {
        if (IsSeasonAndTemperatureLineVisible) yield return MainHudLineKey.SeasonAndTemperature;
        if (IsBodyTemperatureLineVisible)      yield return MainHudLineKey.BodyTemperature;
        if (IsDateAndTimeLineVisible)          yield return MainHudLineKey.DateAndTime;
        if (IsRealtimeLineVisible)             yield return MainHudLineKey.Realtime;
        if (IsWindLineVisible)                 yield return MainHudLineKey.Wind;
        if (IsRainfallLineVisible)             yield return MainHudLineKey.Rainfall;
        if (IsIntoxicationLineVisible)         yield return MainHudLineKey.Intoxication;
        if (IsRiftLineVisible)                 yield return MainHudLineKey.Rift;
        if (IsOnlinePlayersLineVisible)        yield return MainHudLineKey.OnlinePlayers;
    }

    /// <summary>Get the current display string for a line key.</summary>
    public string? GetLineText(MainHudLineKey key) => key switch
    {
        MainHudLineKey.SeasonAndTemperature => SeasonAndTemperatureText,
        MainHudLineKey.BodyTemperature      => BodyTemperatureText,
        MainHudLineKey.DateAndTime          => DateAndTimeText,
        MainHudLineKey.Realtime             => RealtimeText,
        MainHudLineKey.Wind                 => WindText,
        MainHudLineKey.Rainfall             => RainfallText,
        MainHudLineKey.Intoxication         => IntoxicationText,
        MainHudLineKey.Rift                 => RiftText,
        MainHudLineKey.OnlinePlayers        => OnlinePlayersText,
        _ => null,
    };

    // --- Helpers ---

    private string SeasonName() => _season switch
    {
        EnumSeason.Spring => _translate(LangKeys.Hud.SeasonStem + "spring"),
        EnumSeason.Summer => _translate(LangKeys.Hud.SeasonStem + "summer"),
        EnumSeason.Fall   => _translate(LangKeys.Hud.SeasonStem + "fall"),
        EnumSeason.Winter => _translate(LangKeys.Hud.SeasonStem + "winter"),
        _ => string.Empty,
    };

    /// <summary>
    /// Format a Celsius value as a temperature string respecting the user's
    /// Fahrenheit preference. Shared by world-temperature and body-temperature
    /// lines so they always use the same unit.
    /// </summary>
    private string TemperatureString(float celsius)
    {
        float display = _settings.Weather.Fahrenheit
            ? celsius * 9f / 5f + 32f
            : celsius;
        string formatted = string.Format(CultureInfo.InvariantCulture, "{0:0.0}", display);
        return _translateFormat(
            _settings.Weather.Fahrenheit ? LangKeys.Hud.TemperatureFahrenheit : LangKeys.Hud.TemperatureCelsius,
            formatted);
    }

    private bool ShouldShowRift()
    {
        switch (_settings.Rift.Display)
        {
            case RiftDisplay.Always:               return true;
            case RiftDisplay.WorldConfigDependent: return _rift.IsAvailable;
            case RiftDisplay.Hidden:               return false;
            default: return false;
        }
    }
}
