using System;
using HudClock.Configuration;
using HudClock.Core;
using HudClock.Domain.Storms;
using HudClock.Infrastructure.Assets;
using HudClock.Infrastructure.Input;
using HudClock.Resources;
using Vintagestory.API.Client;

namespace HudClock.Presentation.StormHud;

/// <summary>
/// Drives the temporal-storm HUD: 5-second poll of <see cref="IStormService"/>,
/// decides display action via <see cref="StormHudStateMachine"/>, and keeps
/// the view in sync. Registers the Ctrl-[ keybind for manual toggle.
/// </summary>
internal sealed class StormHudController : IDisposable
{
    private const int PollIntervalMs = 5000;

    private readonly ICoreClientAPI _api;
    private readonly HudClockSettings _settings;
    private readonly IStormService _storm;
    private readonly StormHudView _view;
    private readonly ModLog _log;
    private readonly long _tickListenerId;
    private bool _disposed;
    private bool _manuallyHidden;

    public StormHudController(
        ICoreClientAPI api,
        HudClockSettings settings,
        IStormService storm,
        IconCache iconCache,
        KeybindRegistry keybinds,
        ModLog log)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _storm = storm ?? throw new ArgumentNullException(nameof(storm));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        _view = new StormHudView(api, iconCache, log);

        keybinds.Register(
            code: _view.ToggleKeyCombinationCode,
            translationKey: LangKeys.Keybind.StormHud,
            binding: settings.Keybinds.ToggleStormHud,
            handler: ToggleManual);

        _tickListenerId = _api.Event.RegisterGameTickListener(OnPoll, PollIntervalMs);
        OnPoll(0f); // prime
    }

    /// <summary>Call when settings change.</summary>
    public void OnSettingsChanged()
    {
        _manuallyHidden = false; // re-evaluate from scratch
        OnPoll(0f);
    }

    private bool ToggleManual(Vintagestory.API.Client.KeyCombination _)
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
        _api.Event.UnregisterGameTickListener(_tickListenerId);
        _view.TryClose();
        _view.Dispose();
    }
}
