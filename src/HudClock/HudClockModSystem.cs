using System;
using HudClock.Configuration;
using HudClock.Core;
using HudClock.Domain.Calendar;
using HudClock.Domain.Claims;
using HudClock.Domain.Rifts;
using HudClock.Domain.Rooms;
using HudClock.Domain.Storms;
using HudClock.Domain.Time;
using HudClock.Domain.Weather;
using HudClock.Infrastructure.Assets;
using HudClock.Infrastructure.Input;
using HudClock.Infrastructure.Settings;
using HudClock.Presentation.MainHud;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace HudClock;

/// <summary>
/// Composition root and entry point for the HUD Clock mod.
/// </summary>
/// <remarks>
/// Initialization is staged so each service is constructed as soon as its
/// dependencies are available, and no sooner:
/// <list type="bullet">
///   <item><b>StartClientSide</b> — infrastructure (<see cref="ModLog"/>,
///     <see cref="ISettingsStore"/>, <see cref="IconCache"/>,
///     <see cref="KeybindRegistry"/>) whose construction only needs the
///     client API.</item>
///   <item><b>OnPlayerReady</b> — domain services and presentation
///     controllers that require the world.</item>
///   <item><b>OnLeaveWorld</b> — persist settings and dispose presentation
///     + services that own event subscriptions.</item>
/// </list>
/// </remarks>
public sealed class HudClockModSystem : ModSystem
{
    // Infrastructure — constructed at StartClientSide, valid for the entire mod lifetime.
    private ModLog? _log;
    private ISettingsStore? _settingsStore;
    private HudClockSettings? _settings;
    private IconCache? _iconCache;
    private ITimeFormatter? _timeFormatter;
    private KeybindRegistry? _keybinds;
    private ICoreClientAPI? _api;

    // Domain services — constructed at IsPlayerReady, torn down on LeaveWorld.
    private ICalendarService? _calendar;
    private IWeatherService? _weather;
    private IStormService? _storm;
    private IRiftService? _rift;
    private IRoomService? _room;
    private IClaimService? _claim;

    // Presentation — constructed at IsPlayerReady, torn down on LeaveWorld.
    private MainHudController? _mainHud;

    /// <inheritdoc />
    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    /// <inheritdoc />
    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);

        _api = api;
        _log = new ModLog(api.Logger);
        _settingsStore = new JsonSettingsStore(api, _log);
        _settings = _settingsStore.Load();
        _iconCache = new IconCache(api, _log);
        // Wrap Lang.Get in a lambda because the method group is ambiguous between its
        // Lang.Get(string) and Lang.Get(string, params object[]) overloads.
        _timeFormatter = new TimeFormatter(_settings.Time, key => Lang.Get(key));
        _keybinds = new KeybindRegistry(api, _log);

        api.Event.IsPlayerReady += OnPlayerReady;
        api.Event.LeaveWorld += OnLeaveWorld;

        _log.Notification("{0} infrastructure loaded.", Mod.Info.Version);
    }

    private bool OnPlayerReady(ref EnumHandling handling)
    {
        // Guard against multiple PlayerReady firings in a single session.
        if (_calendar is not null) return true;

        ICoreClientAPI api = _api ?? throw new InvalidOperationException("API not initialized before PlayerReady.");
        ModLog log = _log ?? throw new InvalidOperationException("Log not initialized before PlayerReady.");
        HudClockSettings settings = _settings ?? throw new InvalidOperationException("Settings not loaded before PlayerReady.");
        ITimeFormatter formatter = _timeFormatter ?? throw new InvalidOperationException("TimeFormatter not initialized.");
        IconCache iconCache = _iconCache ?? throw new InvalidOperationException("IconCache not initialized.");
        KeybindRegistry keybinds = _keybinds ?? throw new InvalidOperationException("KeybindRegistry not initialized.");

        _calendar = new CalendarService(api);
        _weather = new WeatherService(api);
        _storm = new StormService(api, log);
        _rift = new RiftService(api, log);
        _room = new RoomService(api, log);
        _claim = new ClaimService(api);

        _mainHud = new MainHudController(
            api, settings, formatter,
            _calendar, _weather, _rift, _room, _claim,
            iconCache, keybinds, log);

        log.Notification("Domain services online; main HUD ready.");
        return true;
    }

    private void OnLeaveWorld()
    {
        // Persist settings on world exit so any in-session changes survive a crash
        // of the subsequent main-menu session.
        if (_settings is not null)
        {
            _settingsStore?.Save(_settings);
        }

        // Dispose presentation first (ticks services), then services, then assets.
        _mainHud?.Dispose();
        _mainHud = null;

        _room?.Dispose();
        _room = null;
        _calendar = null;
        _weather = null;
        _storm = null;
        _rift = null;
        _claim = null;

        _iconCache?.Dispose();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _mainHud?.Dispose();
        _room?.Dispose();
        _iconCache?.Dispose();
        base.Dispose();
    }
}
