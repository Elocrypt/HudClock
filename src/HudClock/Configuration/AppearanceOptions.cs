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
    /// User-supplied art. Each icon is resolved in order: a PNG in
    /// <c>ModConfig/hudclock/custom/{hud,room}/</c> (survives mod updates),
    /// then the <c>hudclock:textures/{hud,room}/custom/</c> asset (texture
    /// packs), then the matching <see cref="Modern"/> icon. The shipped
    /// transparent placeholders count as "no art" so an unfilled slot shows
    /// Modern art instead of nothing. See <c>IconCache</c>.
    /// </summary>
    Custom,
}

/// <summary>
/// Text color for the main HUD's lines. Darker options reduce the amount of
/// light emitted by the HUD's static text, which mitigates OLED burn-in risk
/// from a permanently-displayed HUD. The trade-off is contrast: the HUD renders
/// on a semi-transparent dark panel, so darker text is harder to read.
/// </summary>
internal enum HudTextColor
{
    /// <summary>Vanilla bright white. Highest legibility, brightest pixels.</summary>
    White,

    /// <summary>
    /// Roughly half-luminance gray. The recommended middle ground: a clear
    /// reduction in emitted light while staying readable on the shaded panel.
    /// </summary>
    Dim,

    /// <summary>
    /// Near-black. The largest reduction in emitted light, and the lowest
    /// contrast — best paired with a higher HUD background opacity.
    /// </summary>
    Black,
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

    /// <summary>
    /// Color of the main HUD's text lines. Defaults to <see cref="HudTextColor.White"/>
    /// so existing installs are visually unchanged; a missing value in an older
    /// settings file also deserializes to <see cref="HudTextColor.White"/>. Darker
    /// options reduce OLED burn-in risk from static bright text — see
    /// <see cref="HudTextColor"/> for the contrast trade-off.
    /// </summary>
    public HudTextColor HudTextColor { get; set; } = HudTextColor.White;

    /// <summary>
    /// English only: "Autumn" (true) or "Fall" (false) for the third season.
    /// Null (the default, and what older settings files deserialize to) means
    /// automatic from the Windows region; see <c>SeasonWording</c>. No settings
    /// row; set it in the JSON to override. Other locales ignore it.
    /// </summary>
    public bool? UseAutumn { get; set; }
}
