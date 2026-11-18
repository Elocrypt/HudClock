using System;
using HudClock.Configuration;

namespace HudClock.Resources;

/// <summary>
/// Asset paths for every texture the mod loads at runtime. Each path is
/// resolved against the user's current <see cref="IconTheme"/> so Modern
/// and Classic art can be swapped without touching call sites.
/// </summary>
/// <remarks>
/// Using helpers here (rather than string literals at call sites) means a
/// file rename surfaces as a compiler error at the path-building methods
/// instead of a missing-texture crash at render time.
/// </remarks>
internal static class AssetPaths
{
    /// <summary>HUD textures drawn behind the season line and in the storm dialog.</summary>
    public static class Hud
    {
        public static string SeasonSpring(IconTheme theme) => $"hudclock:textures/hud/{Folder(theme)}/spring{Suffix(theme)}.png";
        public static string SeasonSummer(IconTheme theme) => $"hudclock:textures/hud/{Folder(theme)}/summer{Suffix(theme)}.png";
        public static string SeasonFall(IconTheme theme)   => $"hudclock:textures/hud/{Folder(theme)}/fall{Suffix(theme)}.png";
        public static string SeasonWinter(IconTheme theme) => $"hudclock:textures/hud/{Folder(theme)}/winter{Suffix(theme)}.png";
        public static string Storm(IconTheme theme)        => $"hudclock:textures/hud/{Folder(theme)}/tempstorm{Suffix(theme)}.png";
    }

    /// <summary>Small icon textures shown inside the main HUD's room indicator.</summary>
    public static class Room
    {
        /// <summary>Generic "indoors" icon used when the player is in a plain closed room.</summary>
        public static string Generic(IconTheme theme)    => $"hudclock:textures/room/{Folder(theme)}/room.png";
        public static string Cellar(IconTheme theme)     => $"hudclock:textures/room/{Folder(theme)}/cellar.png";
        public static string Greenhouse(IconTheme theme) => $"hudclock:textures/room/{Folder(theme)}/greenhouse.png";
    }

    private const string CustomHudPrefix  = "hudclock:textures/hud/custom/";
    private const string CustomRoomPrefix = "hudclock:textures/room/custom/";

    /// <summary>
    /// For a Custom-theme asset path, return where the player's own file
    /// lives (relative to <c>ModConfig/hudclock/custom/</c>) and which Modern
    /// asset to show when they haven't supplied one.
    /// </summary>
    /// <returns><see langword="false"/> for any non-Custom path.</returns>
    /// <example>
    /// <c>hudclock:textures/hud/custom/fall.png</c> →
    /// <c>hud/fall.png</c>, <c>hudclock:textures/hud/modern/fall-large.png</c>.
    /// </example>
    public static bool TryResolveCustom(string assetPath, out string userRelativePath, out string modernFallbackPath)
    {
        if (assetPath.StartsWith(CustomHudPrefix, StringComparison.Ordinal))
        {
            string file = assetPath.Substring(CustomHudPrefix.Length);           // fall.png
            string stem = file.EndsWith(".png", StringComparison.Ordinal) ? file[..^4] : file;
            userRelativePath = "hud/" + file;
            modernFallbackPath = $"hudclock:textures/hud/{Folder(IconTheme.Modern)}/{stem}{Suffix(IconTheme.Modern)}.png";
            return true;
        }

        if (assetPath.StartsWith(CustomRoomPrefix, StringComparison.Ordinal))
        {
            string file = assetPath.Substring(CustomRoomPrefix.Length);
            userRelativePath = "room/" + file;
            modernFallbackPath = $"hudclock:textures/room/{Folder(IconTheme.Modern)}/{file}";
            return true;
        }

        userRelativePath = string.Empty;
        modernFallbackPath = string.Empty;
        return false;
    }

    /// <summary>
    /// Folder segment for a given theme. Matches the on-disk layout at
    /// <c>src/HudClock/assets/hudclock/textures/{hud,room}/{modern,classic,custom}/</c>.
    /// </summary>
    private static string Folder(IconTheme theme) => theme switch
    {
        IconTheme.Modern => "modern",
        IconTheme.Classic => "classic",
        IconTheme.Custom => "custom",
        _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, "Unknown icon theme."),
    };

    /// <summary>
    /// Filename suffix for HUD season/storm icons. Modern and Classic
    /// historically ship as <c>{name}-large.png</c>; Custom uses the
    /// simpler <c>{name}.png</c> convention so texture-pack authors don't
    /// have to remember the suffix. Room icons are unaffected (they
    /// already use the bare filename across all themes).
    /// </summary>
    private static string Suffix(IconTheme theme) => theme switch
    {
        IconTheme.Custom => "",
        _ => "-large",
    };
}
