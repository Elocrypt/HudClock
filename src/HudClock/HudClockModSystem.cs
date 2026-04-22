using HudClock.Configuration;
using HudClock.Core;
using HudClock.Infrastructure.Assets;
using HudClock.Infrastructure.Settings;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace HudClock;

/// <summary>
/// Composition root and entry point for the HUD Clock mod.
/// </summary>
/// <remarks>
/// Responsible for constructing infrastructure, loading settings, and owning
/// the lifetimes of services that outlive individual dialogs. Domain and
/// presentation services will be added in subsequent implementation passes.
/// </remarks>
public sealed class HudClockModSystem : ModSystem
{
    // All fields nullable because they're only populated on the client side
    // via StartClientSide; on the server this class is never fully initialized.
    private ModLog? _log;
    private ISettingsStore? _settingsStore;
    private HudClockSettings? _settings;
    private IconCache? _iconCache;

    /// <inheritdoc />
    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    /// <inheritdoc />
    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);

        _log = new ModLog(api.Logger);
        _settingsStore = new JsonSettingsStore(api, _log);
        _settings = _settingsStore.Load();
        _iconCache = new IconCache(api, _log);

        api.Event.LeaveWorld += OnLeaveWorld;

        _log.Notification("{0} infrastructure loaded.", Mod.Info.Version);
    }

    private void OnLeaveWorld()
    {
        // Persist settings on world exit so any in-session changes survive a crash
        // of the subsequent main-menu session (unlike 3.x, which only saved on
        // LeaveWorld and lost changes made in the settings dialog if the game crashed).
        if (_settings is not null)
        {
            _settingsStore?.Save(_settings);
        }
        _iconCache?.Dispose();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _iconCache?.Dispose();
        base.Dispose();
    }
}
