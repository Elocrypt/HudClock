using System;
using HudClock.Configuration;
using HudClock.Core;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace HudClock.Infrastructure.Input;

/// <summary>
/// Adapter over <see cref="IInputAPI"/> for registering HUD Clock hotkeys with
/// their stored defaults and capturing the player's current mapping back into
/// a <see cref="Keybind"/> that can be persisted to settings.
/// </summary>
/// <remarks>
/// The registry itself does no file I/O — persistence happens through
/// <see cref="Settings.ISettingsStore"/>.
/// </remarks>
internal sealed class KeybindRegistry
{
    private readonly ICoreClientAPI _api;
    private readonly ModLog _log;

    public KeybindRegistry(ICoreClientAPI api, ModLog log)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <summary>
    /// Register a hotkey with Vintage Story, applying the stored
    /// <paramref name="binding"/> as the default key combination.
    /// </summary>
    /// <param name="code">Unique identifier for this hotkey within the game.</param>
    /// <param name="translationKey">
    /// Lang key for the user-facing label shown in the Controls settings screen
    /// (e.g. <c>hudclock:hudclock-keybind-time-title</c>).
    /// </param>
    /// <param name="binding">Stored key combination used as the default.</param>
    /// <param name="handler">Callback invoked when the user presses the combo.</param>
    public void Register(string code, string translationKey, Keybind binding, ActionConsumable<KeyCombination> handler)
    {
        if (code is null) throw new ArgumentNullException(nameof(code));
        if (translationKey is null) throw new ArgumentNullException(nameof(translationKey));
        if (binding is null) throw new ArgumentNullException(nameof(binding));
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        _api.Input.RegisterHotKey(
            code,
            Lang.Get(translationKey),
            (GlKeys)binding.KeyCode,
            HotkeyType.GUIOrOtherControls,
            binding.Alt,
            binding.Ctrl,
            binding.Shift);
        _api.Input.SetHotKeyHandler(code, handler);
    }

    /// <summary>
    /// Read the player's current live mapping for a previously-registered
    /// hotkey. Returns the binding's stored defaults if the code is unknown.
    /// </summary>
    public Keybind CaptureCurrent(string code)
    {
        if (code is null) throw new ArgumentNullException(nameof(code));

        KeyCombination? mapping = _api.Input.GetHotKeyByCode(code)?.CurrentMapping;
        if (mapping is null)
        {
            _log.Warning("Hotkey '{0}' not registered; cannot capture current mapping.", code);
            return new Keybind();
        }

        return new Keybind
        {
            KeyCode = mapping.KeyCode,
            Alt = mapping.Alt,
            Ctrl = mapping.Ctrl,
            Shift = mapping.Shift,
        };
    }
}
