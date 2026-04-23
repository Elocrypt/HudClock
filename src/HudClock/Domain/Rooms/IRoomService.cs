using System;

namespace HudClock.Domain.Rooms;

/// <summary>
/// Produces an up-to-date <see cref="RoomStatus"/> for the player's current
/// location. Internally throttled by movement and nearby-block-change events;
/// callers query freely without triggering rescans.
/// </summary>
internal interface IRoomService : IDisposable
{
    /// <summary>Current classification of the player's surroundings.</summary>
    RoomStatus CurrentStatus { get; }
}
