using HudClock.Configuration;

namespace HudClock.Infrastructure.Settings;

/// <summary>
/// Load/save contract for <see cref="HudClockSettings"/>. Implementations are
/// responsible for graceful handling of missing or corrupt files — a call to
/// <see cref="Load"/> never throws and always returns a usable object.
/// </summary>
internal interface ISettingsStore
{
    /// <summary>
    /// Load settings from disk. Returns freshly-constructed defaults if the
    /// file is missing or cannot be parsed.
    /// </summary>
    HudClockSettings Load();

    /// <summary>
    /// Persist the given settings to disk. Errors are logged but not thrown,
    /// since save failures during world-leave must never cascade into crashes.
    /// </summary>
    void Save(HudClockSettings settings);
}
