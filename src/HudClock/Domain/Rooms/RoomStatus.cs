namespace HudClock.Domain.Rooms;

/// <summary>Classification of the room the player currently stands in, if any.</summary>
internal enum RoomStatus
{
    /// <summary>Player is not in any valid closed room.</summary>
    None,

    /// <summary>Player is in a valid closed room.</summary>
    Room,

    /// <summary>Player is in a room small enough to qualify as a cellar.</summary>
    SmallRoom,

    /// <summary>Player is in a room with more skylight than non-skylight blocks.</summary>
    Greenhouse,
}
