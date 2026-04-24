using System;
using System.Collections.Generic;
using System.Globalization;
using HudClock.Configuration;
using HudClock.Domain.Calendar;
using HudClock.Domain.Claims;
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

    public MainHudViewModel(
        HudClockSettings settings,
        ITimeFormatter timeFormatter,
        ICalendarService calendar,
        IWeatherService weather,
        IRiftService rift,
        IRoomService room,
        IClaimService claim,
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
    }

    /// <summary>
    /// True when a visible icon (season icon, room-status icon) has changed
    /// since the last time the view baked a static draw. The controller
    /// checks this after each <see cref="Tick"/> and triggers a full view
    /// Rebuild when set, because VS's <c>AddStaticCustomDraw</c> bakes the
    /// bitmap into the composer's cached texture at Compose time and doesn't
    /// re-run the draw callback. Text lines avoid this problem — they use
    /// <c>AddDynamicText</c> and update via <c>SetNewText</c>.
    /// </summary>
    public bool HasVisibleIconChanged =>
        _lastBakedSeason != _season || _lastBakedRoomStatus != _roomStatus;

    /// <summary>
    /// Called by the view after each successful Rebuild to snapshot which
    /// icon values were baked into the current composition. Resets
    /// <see cref="HasVisibleIconChanged"/> until the next change.
    /// </summary>
    public void MarkIconsBaked()
    {
        _lastBakedSeason = _season;
        _lastBakedRoomStatus = _roomStatus;
    }

    // --- Visibility helpers used by the view to decide which lines to lay out. ---

    public bool IsSeasonAndTemperatureLineVisible =>
        _settings.Weather.ShowSeason || _settings.Weather.ShowTemperature;

    public bool IsDateAndTimeLineVisible =>
        _settings.Time.ShowDate || _settings.Time.ShowTime;

    public bool IsRealtimeLineVisible => _settings.Time.ShowRealtime;

    public bool IsWindLineVisible => _settings.Weather.Wind != WindDisplay.Hidden;

    public bool IsRiftLineVisible =>
        ShouldShowRift() && _riftCode is not null;

    public bool IsOnlinePlayersLineVisible =>
        _settings.Multiplayer.ShowOnlinePlayerCount && _isMultiplayer;

    public bool IsRoomIndicatorVisible =>
        _settings.Room.ShowRoomIndicator && _roomStatus != RoomStatus.None;

    /// <summary>True when the HUD has no content to render at all.</summary>
    public bool IsEmpty =>
        !IsSeasonAndTemperatureLineVisible
        && !IsDateAndTimeLineVisible
        && !IsRealtimeLineVisible
        && !IsWindLineVisible
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
                return $"{SeasonName()}, {TemperatureString()}";
            if (showSeason)
                return SeasonName();
            return TemperatureString();
        }
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
        EnumSeason.Spring => AssetPaths.Hud.SeasonSpring,
        EnumSeason.Summer => AssetPaths.Hud.SeasonSummer,
        EnumSeason.Fall   => AssetPaths.Hud.SeasonFall,
        EnumSeason.Winter => AssetPaths.Hud.SeasonWinter,
        _ => null,
    };

    /// <summary>Room-indicator icon path, or null when no indicator is active.</summary>
    public string? RoomIconPath
    {
        get
        {
            if (!_settings.Room.ShowRoomIndicator) return null;
            return _roomStatus switch
            {
                RoomStatus.Greenhouse => AssetPaths.Room.Greenhouse,
                RoomStatus.SmallRoom  => AssetPaths.Room.Cellar,
                RoomStatus.Room       => AssetPaths.Room.Generic,
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
        if (IsDateAndTimeLineVisible)          yield return MainHudLineKey.DateAndTime;
        if (IsRealtimeLineVisible)             yield return MainHudLineKey.Realtime;
        if (IsWindLineVisible)                 yield return MainHudLineKey.Wind;
        if (IsRiftLineVisible)                 yield return MainHudLineKey.Rift;
        if (IsOnlinePlayersLineVisible)        yield return MainHudLineKey.OnlinePlayers;
    }

    /// <summary>Get the current display string for a line key.</summary>
    public string? GetLineText(MainHudLineKey key) => key switch
    {
        MainHudLineKey.SeasonAndTemperature => SeasonAndTemperatureText,
        MainHudLineKey.DateAndTime          => DateAndTimeText,
        MainHudLineKey.Realtime             => RealtimeText,
        MainHudLineKey.Wind                 => WindText,
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

    private string TemperatureString()
    {
        float display = _settings.Weather.Fahrenheit
            ? _temperatureCelsius * 9f / 5f + 32f
            : _temperatureCelsius;
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
