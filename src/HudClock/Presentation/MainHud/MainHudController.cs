using System;
using HudClock.Configuration;
using HudClock.Core;
using HudClock.Domain.Calendar;
using HudClock.Domain.Claims;
using HudClock.Domain.Rifts;
using HudClock.Domain.Rooms;
using HudClock.Domain.Time;
using HudClock.Domain.Weather;
using HudClock.Infrastructure.Assets;
using HudClock.Infrastructure.Input;
using HudClock.Resources;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace HudClock.Presentation.MainHud;

/// <summary>
/// Glue between the main HUD's view, viewmodel, and the VS tick loop.
/// Owns lifetimes, subscribes to setting changes, registers the toggle
/// keybind, and drives the 2.5-second refresh cadence.
/// </summary>
internal sealed class MainHudController : IDisposable
{
    private const int TickIntervalMs = 2500;

    private readonly ICoreClientAPI _api;
    private readonly HudClockSettings _settings;
    private readonly MainHudView _view;
    private readonly MainHudViewModel _viewModel;
    private readonly ModLog _log;
    private readonly long _tickListenerId;
    private bool _disposed;

    public MainHudController(
        ICoreClientAPI api,
        HudClockSettings settings,
        ITimeFormatter timeFormatter,
        ICalendarService calendar,
        IWeatherService weather,
        IRiftService rift,
        IRoomService room,
        IClaimService claim,
        IconCache iconCache,
        KeybindRegistry keybinds,
        ModLog log)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        _viewModel = new MainHudViewModel(
            settings,
            timeFormatter,
            calendar,
            weather,
            rift,
            room,
            claim,
            // Wrap Lang.Get in lambdas — the method group is ambiguous between its
            // Lang.Get(string) and Lang.Get(string, params object[]) overloads, and
            // the two delegates have structurally different signatures anyway.
            translate: key => Lang.Get(key),
            translateFormat: (key, args) => Lang.Get(key, args),
            playerPosition: GetPlayerPos,
            onlinePlayerCount: () => api.World.AllOnlinePlayers.Length,
            isMultiplayer: !api.IsSinglePlayer);

        _view = new MainHudView(api, _viewModel, iconCache, log);

        // Prime layout and register the keybind before showing.
        _viewModel.Tick();
        _view.Rebuild(settings.Display.Anchor);
        _view.TryOpen();

        keybinds.Register(
            code: _view.ToggleKeyCombinationCode,
            translationKey: LangKeys.Keybind.MainHud,
            binding: settings.Keybinds.ToggleMainHud,
            handler: ToggleVisible);

        _tickListenerId = _api.Event.RegisterGameTickListener(OnTick, TickIntervalMs);
    }

    /// <summary>Invoked by <c>ClockModSettingsController.SettingsUpdated</c>.</summary>
    public void OnSettingsChanged()
    {
        _viewModel.Tick();   // refresh cached data against the new settings
        _view.Rebuild(_settings.Display.Anchor);
    }

    private bool ToggleVisible(KeyCombination _)
    {
        if (_view.IsOpened()) _view.TryClose();
        else _view.TryOpen();
        return true;
    }

    private void OnTick(float dt)
    {
        try
        {
            _viewModel.Tick();
            _view.UpdateTexts();
        }
        catch (Exception ex)
        {
            _log.Error("MainHud tick failed: {0}", ex.Message);
        }
    }

    private BlockPos GetPlayerPos()
    {
        // VS 1.22 deprecated Entity.SidedPos in favor of Entity.Pos. Using var here
        // lets the compiler infer whatever concrete type Pos returns without us
        // needing to import its namespace.
        var pos = _api.World?.Player?.Entity?.Pos;
        return pos?.AsBlockPos ?? new BlockPos(0, 0, 0);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _api.Event.UnregisterGameTickListener(_tickListenerId);
        _view.TryClose();
        _view.Dispose();
    }
}
