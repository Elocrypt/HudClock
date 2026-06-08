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
    /// Season derived from the calendar month, independent of the player's
    /// world position. Mar–May = Spring, Jun–Aug = Summer, Sep–Nov = Fall,
    /// Dec–Feb = Winter. Robust against third-party mods that override VS's
    /// latitude or hemisphere delegates.
    /// </summary>
    EnumSeason GetSeason(BlockPos pos);

    /// <summary>
    /// Season at <paramref name="pos"/> using VS's position-based
    /// <c>IGameCalendar.GetSeason</c> API, which accounts for the player's
    /// hemisphere (southern hemisphere players see Fall in May, etc.).
    /// </summary>
    /// <remarks>
    /// May return incorrect results when world-generation mods override VS's
    /// latitude delegates. Prefer <see cref="GetSeason"/> as the default;
    /// use this only when the user has explicitly opted into hemisphere-aware
    /// behavior via <c>AppearanceOptions.HemisphereAwareSeason</c>.
    /// </remarks>
    EnumSeason GetSeasonAt(BlockPos pos);
}
