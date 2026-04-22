using System;
using HudClock.Configuration;
using HudClock.Core;
using Vintagestory.API.Common;

namespace HudClock.Infrastructure.Settings;

/// <summary>
/// Settings store backed by Vintage Story's <c>LoadModConfig&lt;T&gt;</c> and
/// <c>StoreModConfig&lt;T&gt;</c> extension methods on <see cref="ICoreAPI"/>.
/// Writes settings under <c>ModConfig/hudclock/settings.json</c>.
/// </summary>
/// <remarks>
/// This consolidates the three separate JSON files used in 3.x
/// (<c>mod.settings.json</c>, <c>stormdialog.keybinding.json</c>,
/// <c>hudclock.timedialog.json</c>) into a single document.
/// </remarks>
internal sealed class JsonSettingsStore : ISettingsStore
{
    private const string ConfigPath = "hudclock/settings.json";

    private readonly ICoreAPI _api;
    private readonly ModLog _log;

    public JsonSettingsStore(ICoreAPI api, ModLog log)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <inheritdoc />
    public HudClockSettings Load()
    {
        HudClockSettings? loaded = null;

        try
        {
            loaded = _api.LoadModConfig<HudClockSettings>(ConfigPath);
        }
        catch (Exception ex)
        {
            _log.Error("Failed to parse settings ({0}). Using defaults and overwriting on next save.", ex.Message);
            return new HudClockSettings();
        }

        if (loaded is null)
        {
            // First launch for this user: write defaults so the file exists and
            // the user can edit it externally if they wish.
            var defaults = new HudClockSettings();
            Save(defaults);
            return defaults;
        }

        return loaded;
    }

    /// <inheritdoc />
    public void Save(HudClockSettings settings)
    {
        if (settings is null) throw new ArgumentNullException(nameof(settings));

        try
        {
            _api.StoreModConfig(settings, ConfigPath);
        }
        catch (Exception ex)
        {
            _log.Error("Failed to save settings: {0}", ex.Message);
        }
    }
}
