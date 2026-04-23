namespace HudClock.Domain.Calendar;

/// <summary>
/// Immutable snapshot of the in-game calendar at a point in time. Viewmodels
/// consume this record instead of holding a live reference to the game's
/// <c>IGameCalendar</c>, which keeps them unit-testable.
/// </summary>
/// <param name="DayOfMonth">1-based day of the current month.</param>
/// <param name="MonthKey">
/// Lowercase month identifier suitable for building a lang key
/// (e.g. <c>"january"</c> → <c>"month-january"</c>). Kept as a plain string so
/// consumers don't take a dependency on <c>Vintagestory.API.Common.EnumMonth</c>.
/// </param>
/// <param name="Year">Current calendar year.</param>
/// <param name="Hour">Hour of day, 0-23.</param>
/// <param name="Minute">Minute of hour, 0-59.</param>
internal readonly record struct CalendarSnapshot(
    int DayOfMonth,
    string MonthKey,
    int Year,
    int Hour,
    int Minute);
