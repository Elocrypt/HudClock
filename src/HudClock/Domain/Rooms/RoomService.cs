using System;
using HudClock.Core;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HudClock.Domain.Rooms;

/// <summary>
/// Production <see cref="IRoomService"/>. Scans the player's current position
/// against <see cref="RoomRegistry"/> with adaptive cadence:
/// <list type="bullet">
///   <item>Fast poll while moving, slow while stationary.</item>
///   <item>Rescans on nearby block changes, throttled to avoid storms during
///         bulk edits.</item>
///   <item>Skips rescans entirely while the player remains in the cached
///         room's bounding box and nothing has invalidated the cache.</item>
/// </list>
/// </summary>
internal sealed class RoomService : IRoomService
{
    // Tick timing, in milliseconds.
    private const int TickIntervalMs = 100;
    private const int ScanWhileMovingMs = 250;
    private const int ScanWhileIdleMs = 750;
    private const int BlockChangeThrottleMs = 600;

    // Radius in blocks around the player within which a block change triggers a rescan.
    private const int NearChangeRadiusXZ = 12;
    private const int NearChangeRadiusY = 6;

    private readonly ICoreClientAPI _api;
    private readonly ModLog _log;
    private readonly long _tickListenerId;

    private BlockPos? _lastPlayerPos;
    private Room? _cachedRoom;
    private float _msSinceScan;
    private float _msSinceNearChange;
    private bool _dirty;
    private bool _disposed;

    public RoomService(ICoreClientAPI api, ModLog log)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _log = log ?? throw new ArgumentNullException(nameof(log));

        _tickListenerId = _api.Event.RegisterGameTickListener(OnTick, TickIntervalMs);
        _api.Event.BlockChanged += OnBlockChanged;
    }

    /// <inheritdoc />
    public RoomStatus CurrentStatus { get; private set; } = RoomStatus.None;

    private void OnBlockChanged(BlockPos pos, Block oldBlock)
    {
        // Only rescans if the change is within a small neighborhood of the player;
        // this keeps the service cheap during large-scale edits far from the HUD user.
        // VS 1.22 deprecated Entity.SidedPos in favor of Entity.Pos.
        var playerPos = _api.World?.Player?.Entity?.Pos?.AsBlockPos;
        if (playerPos is null) return;

        if (Math.Abs(pos.X - playerPos.X) <= NearChangeRadiusXZ &&
            Math.Abs(pos.Y - playerPos.Y) <= NearChangeRadiusY &&
            Math.Abs(pos.Z - playerPos.Z) <= NearChangeRadiusXZ)
        {
            _dirty = true;
            _msSinceNearChange = 0f;
        }
    }

    private void OnTick(float dt)
    {
        _msSinceScan += dt * 1000f;
        _msSinceNearChange += dt * 1000f;

        var entity = _api.World?.Player?.Entity;
        if (entity?.Pos is null)
        {
            Reset();
            return;
        }

        BlockPos currentPos = entity.Pos.AsBlockPos;
        bool moved = _lastPlayerPos is null || !currentPos.Equals(_lastPlayerPos);
        _lastPlayerPos = currentPos;

        // Fast path: still inside the cached room, nothing near has changed — no rescan needed.
        if (!moved && !_dirty && _cachedRoom is not null && IsInsideAabb(_cachedRoom.Location, currentPos))
        {
            return;
        }

        // Determine whether we're due for a scan this tick.
        bool blockChangeTriggered = _dirty && _msSinceNearChange >= BlockChangeThrottleMs;
        int cadenceMs = moved ? ScanWhileMovingMs : ScanWhileIdleMs;
        bool cadenceDue = _msSinceScan >= cadenceMs;

        if (!blockChangeTriggered && !cadenceDue) return;

        _msSinceScan = 0f;
        _dirty = false;
        Scan(currentPos);
    }

    private void Scan(BlockPos pos)
    {
        var registry = _api.World?.Api?.ModLoader?.GetModSystem<RoomRegistry>();
        if (registry is null)
        {
            Reset();
            return;
        }

        // RoomRegistry.GetRoomForPosition occasionally throws on chunk-boundary
        // edge cases; a try-catch keeps the HUD alive. Fall back to querying
        // adjacent Y levels in case the player is straddling two rooms.
        Room? room = SafeGet(registry, pos);
        if (!IsValidClosedRoom(room))
        {
            room = SafeGet(registry, new BlockPos(pos.X, pos.Y + 1, pos.Z, pos.dimension))
                ?? SafeGet(registry, new BlockPos(pos.X, pos.Y - 1, pos.Z, pos.dimension));
        }

        if (!IsValidClosedRoom(room))
        {
            Reset();
            return;
        }

        _cachedRoom = room;
        CurrentStatus = Classify(room!);
    }

    private static RoomStatus Classify(Room room)
    {
        if (room.SkylightCount > room.NonSkylightCount) return RoomStatus.Greenhouse;
        if (room.IsSmallRoom) return RoomStatus.SmallRoom;
        return RoomStatus.Room;
    }

    private Room? SafeGet(RoomRegistry reg, BlockPos pos)
    {
        try { return reg.GetRoomForPosition(pos); }
        catch (Exception ex) { _log.Debug("RoomRegistry lookup threw: {0}", ex.Message); return null; }
    }

    private static bool IsValidClosedRoom(Room? room)
    {
        if (room is null || room.ExitCount > 0 || room.Location is null) return false;
        // Reject the degenerate 1x1x1 "room" the registry returns for open space.
        int sx = room.Location.X2 - room.Location.X1 + 1;
        int sy = room.Location.Y2 - room.Location.Y1 + 1;
        int sz = room.Location.Z2 - room.Location.Z1 + 1;
        return !(sx == 1 && sy == 1 && sz == 1);
    }

    private static bool IsInsideAabb(Cuboidi? loc, BlockPos p)
    {
        if (loc is null) return false;
        return p.X >= loc.X1 && p.X <= loc.X2
            && p.Y >= loc.Y1 && p.Y <= loc.Y2
            && p.Z >= loc.Z1 && p.Z <= loc.Z2;
    }

    private void Reset()
    {
        CurrentStatus = RoomStatus.None;
        _cachedRoom = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _api.Event.UnregisterGameTickListener(_tickListenerId);
        _api.Event.BlockChanged -= OnBlockChanged;
    }
}
