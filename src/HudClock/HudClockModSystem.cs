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
using HudClock.Presentation.ClaimHud;
using HudClock.Presentation.MainHud;
using HudClock.Presentation.SettingsDialog;
using HudClock.Presentation.StormHud;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace HudClock;

/// <summary>
/// Composition root and entry point for the HUD Clock mod.
/// </summary>
public sealed class HudClockModSystem : ModSystem
{
    // Infrastructure.
    private ModLog? _log;
    private ISettingsStore? _settingsStore;
    private HudClockSettings? _settings;
    private IconCache? _iconCache;
    private ITimeFormatter? _timeFormatter;
    private KeybindRegistry? _keybinds;
    private ICoreClientAPI? _api;

    // Domain services.
    private ICalendarService? _calendar;
    private IWeatherService? _weather;
    private IStormService? _storm;
    private IRiftService? _rift;
    private IRoomService? _room;
    private IClaimService? _claim;

    // Presentation controllers.
    private MainHudController? _mainHud;
    private StormHudController? _stormHud;
    private ClaimHudController? _claimHud;
    private SettingsDialogController? _settingsDialog;

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
        _timeFormatter = new TimeFormatter(_settings.Time, key => Lang.Get(key));
        _keybinds = new KeybindRegistry(api, _log);

        api.Event.IsPlayerReady += OnPlayerReady;
        api.Event.LeaveWorld += OnLeaveWorld;

        _log.Notification("{0} infrastructure loaded.", Mod.Info.Version);
    }

    private bool OnPlayerReady(ref EnumHandling handling)
    {
        if (_calendar is not null) return true;

        ICoreClientAPI api = _api ?? throw new InvalidOperationException("API not initialized.");
        ModLog log = _log ?? throw new InvalidOperationException("Log not initialized.");
        HudClockSettings settings = _settings ?? throw new InvalidOperationException("Settings not loaded.");
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

        _stormHud = new StormHudController(api, settings, _storm, iconCache, keybinds, log);
        _claimHud = new ClaimHudController(api, settings, _claim, log);
        _settingsDialog = new SettingsDialogController(api, settings, keybinds, log);

        _settingsDialog.SettingsChanged += OnSettingsChanged;

        log.Notification("Domain services and HUD components online.");
        return true;
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        try
        {
            _mainHud?.OnSettingsChanged();
            _stormHud?.OnSettingsChanged();
            _claimHud?.OnSettingsChanged();
        }
        catch (Exception ex)
        {
            _log?.Error("OnSettingsChanged propagation failed: {0}", ex.Message);
        }
    }

    private void OnLeaveWorld()
    {
        // Capture player's current keybind mappings back into settings.
        if (_keybinds is not null && _settings is not null)
        {
            try
            {
                _settings.Keybinds.OpenSettings   = _keybinds.CaptureCurrent("hudclock:settingsdialog");
                _settings.Keybinds.ToggleMainHud  = _keybinds.CaptureCurrent("hudclock:mainhud");
                _settings.Keybinds.ToggleStormHud = _keybinds.CaptureCurrent("hudclock:stormhud");
            }
            catch (Exception ex)
            {
                _log?.Error("Keybind capture failed: {0}", ex.Message);
            }
        }

        if (_settings is not null) _settingsStore?.Save(_settings);

        // Tear down presentation first, then services, then shared assets.
        _settingsDialog?.Dispose();  _settingsDialog = null;
        _claimHud?.Dispose();        _claimHud = null;
        _stormHud?.Dispose();        _stormHud = null;
        _mainHud?.Dispose();         _mainHud = null;

        _room?.Dispose();            _room = null;
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
        _settingsDialog?.Dispose();
        _claimHud?.Dispose();
        _stormHud?.Dispose();
        _mainHud?.Dispose();
        _room?.Dispose();
        _iconCache?.Dispose();
        base.Dispose();
    }
}
