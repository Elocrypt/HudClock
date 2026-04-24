namespace HudClock.Configuration;

/// <summary>
/// Root configuration container for the HUD Clock mod. Persisted to disk as a single
/// JSON document at <c>ModConfig/hudclock/settings.json</c>.
/// </summary>
/// <remarks>
/// Adding a new option group: create a new Options class in this folder, add a
/// property here with a <c>new()</c> default, and bump <see cref="SchemaVersion"/>
/// only if existing saved files need migration.
/// </remarks>
internal sealed class HudClockSettings
{
    /// <summary>Schema version of this settings document. Incremented when the shape changes.</summary>
    public int SchemaVersion { get; set; } = 1;

    public DisplayOptions Display { get; set; } = new();
    public AppearanceOptions Appearance { get; set; } = new();
    public TimeOptions Time { get; set; } = new();
    public WeatherOptions Weather { get; set; } = new();
    public StormOptions Storm { get; set; } = new();
    public RiftOptions Rift { get; set; } = new();
    public ClaimOptions Claim { get; set; } = new();
    public MultiplayerOptions Multiplayer { get; set; } = new();
    public RoomOptions Room { get; set; } = new();
    public KeybindOptions Keybinds { get; set; } = new();
}
