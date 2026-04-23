using System;
using HudClock.Configuration;
using HudClock.Core;
using HudClock.Domain.Claims;
using Vintagestory.API.Client;

namespace HudClock.Presentation.ClaimHud;

/// <summary>
/// Drives the claim banner: polls the claim service at a low cadence and
/// shows/hides the view accordingly. Respects
/// <see cref="ClaimOptions.ShowClaimedArea"/>.
/// </summary>
internal sealed class ClaimHudController : IDisposable
{
    private const int PollIntervalMs = 3000;

    private readonly ICoreClientAPI _api;
    private readonly HudClockSettings _settings;
    private readonly IClaimService _claim;
    private readonly ClaimHudView _view;
    private readonly ModLog _log;
    private long _tickListenerId;
    private bool _disposed;

    public ClaimHudController(
        ICoreClientAPI api,
        HudClockSettings settings,
        IClaimService claim,
        ModLog log)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _claim = claim ?? throw new ArgumentNullException(nameof(claim));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        _view = new ClaimHudView(api, log);
        ApplyEnabledState();
    }

    /// <summary>Called by the mod system when settings change.</summary>
    public void OnSettingsChanged() => ApplyEnabledState();

    private void ApplyEnabledState()
    {
        bool enabled = _settings.Claim.ShowClaimedArea;

        if (enabled && _tickListenerId == 0)
        {
            _tickListenerId = _api.Event.RegisterGameTickListener(OnPoll, PollIntervalMs);
            OnPoll(0f); // prime immediately
        }
        else if (!enabled && _tickListenerId != 0)
        {
            _api.Event.UnregisterGameTickListener(_tickListenerId);
            _tickListenerId = 0;
            _view.Hide();
        }
    }

    private void OnPoll(float dt)
    {
        try
        {
            var pos = _api.World?.Player?.Entity?.Pos?.AsBlockPos;
            if (pos is null) { _view.Hide(); return; }

            ClaimInfo? info = _claim.GetClaimAt(pos);
            if (info is null) _view.Hide();
            else _view.SetOwner(info.Value.OwnerName);
        }
        catch (Exception ex)
        {
            _log.Error("Claim poll failed: {0}", ex.Message);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_tickListenerId != 0)
        {
            _api.Event.UnregisterGameTickListener(_tickListenerId);
            _tickListenerId = 0;
        }
        _view.TryClose();
        _view.Dispose();
    }
}
