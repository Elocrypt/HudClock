using System;
using HudClock.Configuration;
using HudClock.Core;
using HudClock.Domain.Storms;
using HudClock.Infrastructure.Assets;
using HudClock.Infrastructure.Input;
using HudClock.Presentation.MainHud;
using HudClock.Presentation.Shared;
using HudClock.Resources;
using Vintagestory.API.Client;

namespace HudClock.Presentation.StormHud;

/// <summary>
/// Drives the temporal-storm HUD: periodic poll of <see cref="IStormService"/>,
/// decides display action via <see cref="StormHudStateMachine"/>, and keeps
/// the view in sync. Positions itself via a fixed anchor-offset table, with
/// an extra offset applied when it shares an anchor with the main HUD so it
/// stacks cleanly below Main.
/// </summary>
internal sealed class StormHudController : IDisposable
{
    // Poll at 1s so the approaching-storm countdown (rendered as hh:mm of
    // in-game time) updates at in-game-minute resolution. VS's default world
    // speed is ~60x real-time, so one real second equals one in-game minute.
    // GetStatus is cheap (compiled-delegate field access + two GetValues),
    // so polling every second has no meaningful cost.
    private const int PollIntervalMs = 1000;

    private readonly ICoreClientAPI _api;
    private readonly HudClockSettings _settings;
    private readonly IStormService _storm;
    private readonly StormHudView _view;
    private readonly MainHudController _main;
    private readonly ModLog _log;
    private readonly long _pollTickId;
    private bool _disposed;
    private bool _manuallyHidden;

    public StormHudController(
        ICoreClientAPI api,
        HudClockSettings settings,
        IStormService storm,
        MainHudController main,
        IconCache iconCache,
        KeybindRegistry keybinds,
        ModLog log)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _storm = storm ?? throw new ArgumentNullException(nameof(storm));
        _main = main ?? throw new ArgumentNullException(nameof(main));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        _view = new StormHudView(api, iconCache, settings, log, settings.Storm.Anchor);

        // Initial layout pass using the current offset, before the keybind
        // fires or the first poll. Without this the view sits at offset 0 on
        // first frame (set by the view's constructor default) even if Main
        // is already in the same corner.
        ApplyOffset();

        keybinds.Register(
            code: _view.ToggleKeyCombinationCode,
            translationKey: LangKeys.Keybind.StormHud,
            binding: settings.Keybinds.ToggleStormHud,
            handler: ToggleManual);

        _pollTickId = _api.Event.RegisterGameTickListener(OnPoll, PollIntervalMs);
        _main.LayoutChanged += OnMainLayoutChanged;
        OnPoll(0f); // prime
    }

    /// <summary>Call when mod settings change.</summary>
    public void OnSettingsChanged()
    {
        // ApplyOffset below reads _settings.Storm.Anchor so an anchor change
        // rebuilds the view with the fresh anchor. If only visibility policy
        // changed, ApplyOffset is a cheap rebuild with the same numbers — and
        // the subsequent OnPoll reflects any policy change in visibility.
        ApplyOffset();
        _manuallyHidden = false;
        OnPoll(0f);
    }

    /// <summary>Reacts to Main's layout changing (new anchor or size).</summary>
    private void OnMainLayoutChanged(object? sender, EventArgs e)
    {
        // Only the stacking offset changes when Main moves; Storm's anchor
        // stays whatever the user configured for it. Re-apply.
        ApplyOffset();
    }

    /// <summary>
    /// Compute the correct total offset for Storm's current anchor (solo +
    /// optional stacking below Main if they share an anchor) and rebuild the
    /// view with that offset.
    /// </summary>
    /// <remarks>
    /// Stacking direction depends on anchor edge: for top anchors, Storm sits
    /// below Main on-screen, which means a <i>more-positive</i> offset. For
    /// bottom anchors, Storm sits above Main on-screen, which means a
    /// <i>more-negative</i> offset (offsets grow away from the anchor edge).
    /// </remarks>
    private void ApplyOffset()
    {
        HudAnchor anchor = _settings.Storm.Anchor;
        double solo = HudAnchorOffsets.GetSoloOffsetY(anchor);

        double stacking = 0.0;
        if (anchor == _main.CurrentAnchor && _main.CurrentLogicalOuterHeight > 0)
        {
            double magnitude = _main.CurrentLogicalOuterHeight + HudAnchorOffsets.InterHudPadding;
            stacking = HudAnchorOffsets.IsBottomAnchor(anchor) ? -magnitude : +magnitude;
        }

        _view.Rebuild(anchor, solo + stacking);
    }

    private bool ToggleManual(KeyCombination _)
    {
        if (_view.IsOpened())
        {
            _view.Apply(StormHudAction.Hidden, default);
            _manuallyHidden = true;
        }
        else
        {
            _manuallyHidden = false;
            OnPoll(0f);
        }
        return true;
    }

    private void OnPoll(float dt)
    {
        try
        {
            if (_manuallyHidden) return;

            StormStatus status = _storm.GetStatus();
            StormHudAction action = StormHudStateMachine.Decide(status, _settings.Storm.Display);
            _view.Apply(action, status);
        }
        catch (Exception ex)
        {
            _log.Error("Storm poll failed: {0}", ex.Message);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _main.LayoutChanged -= OnMainLayoutChanged;
        _api.Event.UnregisterGameTickListener(_pollTickId);
        _view.TryClose();
        _view.Dispose();
    }
}
