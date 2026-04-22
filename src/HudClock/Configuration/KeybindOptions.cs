namespace HudClock.Configuration;

/// <summary>
/// Serializable representation of a single keybind. Persists the primary key code
/// and the three modifier-key states.
/// </summary>
/// <remarks>
/// <see cref="KeyCode"/> values match Vintage Story's <c>GlKeys</c> enum; storing
/// them as plain <see cref="int"/> keeps the settings JSON portable across game
/// versions should the enum's backing numbers ever shift.
/// </remarks>
internal sealed class Keybind
{
    public int KeyCode { get; set; }
    public bool Alt { get; set; }
    public bool Ctrl { get; set; }
    public bool Shift { get; set; }

    public Keybind() { }

    public Keybind(int keyCode, bool alt = false, bool ctrl = false, bool shift = false)
    {
        KeyCode = keyCode;
        Alt = alt;
        Ctrl = ctrl;
        Shift = shift;
    }
}

/// <summary>
/// All configurable mod keybinds, consolidated from the three per-dialog JSON
/// files used in 3.x into a single persisted structure.
/// </summary>
internal sealed class KeybindOptions
{
    /// <summary>Opens the mod settings dialog. Default: <c>Shift+A</c>.</summary>
    public Keybind OpenSettings { get; set; } = new(keyCode: 97, shift: true);

    /// <summary>Toggles the main HUD on/off. Default: <c>Ctrl+G</c>.</summary>
    public Keybind ToggleMainHud { get; set; } = new(keyCode: 103, ctrl: true);

    /// <summary>Toggles the temporal storm dialog on/off. Default: <c>Ctrl+[</c>.</summary>
    public Keybind ToggleStormHud { get; set; } = new(keyCode: 91, ctrl: true);
}
