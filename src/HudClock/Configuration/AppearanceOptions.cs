namespace HudClock.Configuration;

/// <summary>
/// Icon art theme used by the main HUD and storm dialog. Season, storm,
/// and room icons all switch together when this changes.
/// </summary>
internal enum IconTheme
{
    /// <summary>Refreshed 4.x art. Larger, more detailed.</summary>
    Modern,

    /// <summary>Original 3.x art. Simpler and pixel-matched to the 3.x layout.</summary>
    Classic,

    /// <summary>
    /// User-supplied art under <c>assets/hudclock/textures/{hud,room}/custom/</c>.
    /// The mod ships transparent placeholders so the HUD layout still
    /// resolves cleanly when no custom art is present; users override
    /// individual files via a standard Vintage Story texture pack or by
    /// dropping PNGs into the custom/ folders directly. Reserves the same
    /// 200×200 season-icon column as <see cref="Modern"/>.
    /// </summary>
    Custom,
}

/// <summary>Options controlling visual appearance unrelated to what the HUD shows.</summary>
internal sealed class AppearanceOptions
{
    /// <summary>
    /// Which set of icon art to use for seasons, storms, and room indicators.
    /// Defaults to <see cref="IconTheme.Modern"/> on fresh installs so new
    /// users see the refreshed art. Existing users who kept their 4.0
    /// settings file will also get Modern because the missing
    /// <see cref="AppearanceOptions"/> section deserializes to default values.
    /// </summary>
    public IconTheme IconTheme { get; set; } = IconTheme.Modern;

    /// <summary>
    /// When <see langword="true"/> (default), the season icon and label use VS's
    /// position-based <c>IGameCalendar.GetSeason</c> API, which returns the
    /// hemisphere-correct season — southern hemisphere players see Fall in May,
    /// etc. This is the vanilla-accurate behavior.
    /// <para>
    /// Set to <see langword="false"/> to derive the season from the calendar
    /// month instead (Mar–May = Spring, Jun–Aug = Summer, Sep–Nov = Fall,
    /// Dec–Feb = Winter). The month-based path is unaffected by world-generation
    /// mods that override VS's latitude delegates, so turn this off if a
    /// world-gen mod (notably Realistic Biome Generation) is causing the wrong
    /// season to display on a fresh world.
    /// </para>
    /// </summary>
    public bool HemisphereAwareSeason { get; set; } = true;
}
