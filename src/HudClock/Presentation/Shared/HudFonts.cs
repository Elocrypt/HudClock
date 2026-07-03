using HudClock.Configuration;
using Vintagestory.API.Client;

namespace HudClock.Presentation.Shared;

/// <summary>
/// Builds the line font shared by the HUD surfaces (main HUD and storm HUD).
/// Centralizing it here keeps the text-color values identical across both, so
/// changing the appearance setting can't leave the two HUDs looking different.
/// </summary>
/// <remarks>
/// Starts from VS's detail-text font and swaps <em>only</em> the color per the
/// appearance setting; size and weight are untouched so line metrics are
/// identical across all options. Colors are Cairo RGBA in the 0..1 range. The
/// HUDs render on a semi-transparent dark panel, so <see cref="HudTextColor.Black"/>
/// emits the least light (best for OLED burn-in) at the cost of contrast, while
/// <see cref="HudTextColor.Dim"/> roughly halves luminance yet stays readable —
/// the recommended middle ground. <see cref="CairoFont.WithColor"/> clones the
/// array, so the returned instance is safe to reuse across many lines.
/// </remarks>
internal static class HudFonts
{
    public static CairoFont Line(HudTextColor color) => color switch
    {
        HudTextColor.Dim   => CairoFont.WhiteDetailText().WithColor(new[] { 0.55, 0.55, 0.55, 1.0 }),
        HudTextColor.Black => CairoFont.WhiteDetailText().WithColor(new[] { 0.08, 0.08, 0.08, 1.0 }),
        _                  => CairoFont.WhiteDetailText(),
    };
}
