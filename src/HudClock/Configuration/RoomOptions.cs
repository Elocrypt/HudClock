namespace HudClock.Configuration;

/// <summary>Options controlling the room/shelter indicator.</summary>
internal sealed class RoomOptions
{
    /// <summary>When true, display a room/cellar/greenhouse icon whenever the player stands inside a valid closed room.</summary>
    public bool ShowRoomIndicator { get; set; } = true;
}
