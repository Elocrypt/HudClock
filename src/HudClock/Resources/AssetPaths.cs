namespace HudClock.Resources;

/// <summary>
/// Asset paths for every texture the mod loads at runtime. Using constants
/// here (rather than string literals at call sites) means renaming a file
/// surfaces as a compiler error, not a missing-texture crash at runtime.
/// </summary>
internal static class AssetPaths
{
    /// <summary>HUD season-background textures keyed by a lowercase season identifier.</summary>
    public static class Hud
    {
        public const string SeasonSpring = "hudclock:textures/hud/spring-large.png";
        public const string SeasonSummer = "hudclock:textures/hud/summer-large.png";
        public const string SeasonFall   = "hudclock:textures/hud/fall-large.png";
        public const string SeasonWinter = "hudclock:textures/hud/winter-large.png";
        public const string Storm        = "hudclock:textures/hud/tempstorm-large.png";
    }

    /// <summary>Small icon textures shown inside the main HUD.</summary>
    public static class Room
    {
        /// <summary>Generic "indoors" icon used when the player is in a plain closed room.</summary>
        public const string Generic    = "hudclock:textures/room/room.png";
        public const string Cellar     = "hudclock:textures/room/cellar.png";
        public const string Greenhouse = "hudclock:textures/room/greenhouse.png";
    }
}
