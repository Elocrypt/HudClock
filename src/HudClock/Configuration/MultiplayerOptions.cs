namespace HudClock.Configuration;

/// <summary>Options that only apply in multiplayer sessions.</summary>
internal sealed class MultiplayerOptions
{
    /// <summary>Show the "online players: N" line. Automatically hidden in singleplayer regardless of this setting.</summary>
    public bool ShowOnlinePlayerCount { get; set; } = true;
}
