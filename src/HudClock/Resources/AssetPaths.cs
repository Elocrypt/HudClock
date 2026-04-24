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
        public static string SeasonSpring(IconTheme theme) => $"hudclock:textures/hud/{Folder(theme)}/spring-large.png";
        public static string SeasonSummer(IconTheme theme) => $"hudclock:textures/hud/{Folder(theme)}/summer-large.png";
        public static string SeasonFall(IconTheme theme)   => $"hudclock:textures/hud/{Folder(theme)}/fall-large.png";
        public static string SeasonWinter(IconTheme theme) => $"hudclock:textures/hud/{Folder(theme)}/winter-large.png";
        public static string Storm(IconTheme theme)        => $"hudclock:textures/hud/{Folder(theme)}/tempstorm-large.png";
    }

    /// <summary>Small icon textures shown inside the main HUD's room indicator.</summary>
    public static class Room
    {
        /// <summary>Generic "indoors" icon used when the player is in a plain closed room.</summary>
        public static string Generic(IconTheme theme)    => $"hudclock:textures/room/{Folder(theme)}/room.png";
        public static string Cellar(IconTheme theme)     => $"hudclock:textures/room/{Folder(theme)}/cellar.png";
        public static string Greenhouse(IconTheme theme) => $"hudclock:textures/room/{Folder(theme)}/greenhouse.png";
    }

    /// <summary>
    /// Folder segment for a given theme. Matches the on-disk layout at
    /// <c>src/HudClock/assets/hudclock/textures/{hud,room}/{modern,classic}/</c>.
    /// </summary>
    private static string Folder(IconTheme theme) => theme switch
    {
        IconTheme.Modern => "modern",
        IconTheme.Classic => "classic",
        _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, "Unknown icon theme."),
    };
}
