using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HudClock.Domain.Calendar;

/// <summary>
/// Read access to the game's calendar state, decoupled from
/// <c>Vintagestory.API.Common.IGameCalendar</c> so viewmodels can be tested with
/// hand-rolled snapshots.
/// </summary>
internal interface ICalendarService
{
    /// <summary>Current calendar snapshot (date + wall-clock time).</summary>
    CalendarSnapshot GetSnapshot();

    /// <summary>
    /// Season at the given block position. Delegates to
    /// <c>IGameCalendar.GetSeason(BlockPos)</c>; replaces the zero-volume
    /// <c>BlockAccessor.SearchBlocks</c> workaround from 3.x.
    /// </summary>
    EnumSeason GetSeason(BlockPos pos);
}
