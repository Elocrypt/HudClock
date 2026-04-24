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
/// keybind, and drives the data refresh cadence.
/// </summary>
/// <remarks>
/// Notifies subscribers of <see cref="LayoutChanged"/> every time the view's
/// layout is rebuilt (new anchor, new line set). Storm HUD subscribes to
/// this so it can re-stack itself below Main when they share an anchor.
/// </remarks>
internal sealed class MainHudController : IDisposable
{
    private const int DataTickMs = 2500;

    private readonly ICoreClientAPI _api;
    private readonly HudClockSettings _settings;
    private readonly MainHudView _view;
    private readonly MainHudViewModel _viewModel;
    private readonly ModLog _log;
    private readonly long _dataTickId;
    private bool _disposed;

    /// <summary>
    /// Raised after each <see cref="MainHudView.Rebuild"/> completes — gives
    /// sibling HUDs (Storm) a chance to re-stack themselves based on our new
    /// size or anchor. Always raised on the main thread (inside a settings-
    /// changed handler or constructor).
    /// </summary>
    public event EventHandler? LayoutChanged;

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

        _dataTickId = _api.Event.RegisterGameTickListener(OnDataTick, DataTickMs);
    }

    /// <summary>Current anchor — sibling HUDs use this to decide whether to stack.</summary>
    public HudAnchor CurrentAnchor => _view.CurrentAnchor;

    /// <summary>
    /// Outer height of the current composition in <b>logical</b> pixels
    /// (pre-GUI-scale). Zero when the view isn't currently showing. Sibling
    /// HUDs use this to compute their stacking offset below us. See
    /// <see cref="MainHudView.CurrentLogicalOuterHeight"/> for the rationale
    /// behind exposing a logical-pixel height rather than VS's post-compose
    /// scaled bounds.
    /// </summary>
    public double CurrentLogicalOuterHeight => _view.CurrentLogicalOuterHeight;

    /// <summary>Invoked by the mod system when settings change.</summary>
    public void OnSettingsChanged()
    {
        _viewModel.Tick();
        _view.Rebuild(_settings.Display.Anchor);
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool ToggleVisible(KeyCombination _)
    {
        if (_view.IsOpened()) _view.TryClose();
        else _view.TryOpen();
        // Visibility change affects our outer height (0 when closed) — let Storm know.
        LayoutChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void OnDataTick(float dt)
    {
        try
        {
            _viewModel.Tick();
            // Icons (season, room status) are baked into VS's static-custom-draw
            // cache at Compose time and do not re-run the draw callback on their
            // own. Detect a state change and trigger a full Rebuild so the new
            // icon gets baked in. Text lines update via UpdateTexts without a
            // rebuild, so in the common "just ticking text" path we stay on the
            // fast path.
            if (_viewModel.HasVisibleIconChanged)
            {
                _view.Rebuild(_settings.Display.Anchor);
                LayoutChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _view.UpdateTexts();
            }
        }
        catch (Exception ex)
        {
            _log.Error("MainHud data tick failed: {0}", ex.Message);
        }
    }

    private BlockPos GetPlayerPos()
    {
        var pos = _api.World?.Player?.Entity?.Pos;
        return pos?.AsBlockPos ?? new BlockPos(0, 0, 0);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _api.Event.UnregisterGameTickListener(_dataTickId);
        _view.TryClose();
        _view.Dispose();
    }
}
