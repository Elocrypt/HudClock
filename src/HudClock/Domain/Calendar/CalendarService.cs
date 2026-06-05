using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HudClock.Domain.Calendar;

/// <summary>Production <see cref="ICalendarService"/> backed by <see cref="ICoreClientAPI.World"/>.</summary>
internal sealed class CalendarService : ICalendarService
{
    private readonly ICoreClientAPI _api;

    public CalendarService(ICoreClientAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    /// <inheritdoc />
    public CalendarSnapshot GetSnapshot()
    {
        IGameCalendar calendar = _api.World.Calendar;

        // TotalDays is a running count of in-game days since epoch. DayOfMonth is 1-based.
        int dayOfMonth = (int)(calendar.TotalDays % calendar.DaysPerMonth) + 1;

        float hourOfDay = calendar.HourOfDay;
        int hour = (int)hourOfDay;
        int minute = (int)((hourOfDay - hour) * 60f);

        return new CalendarSnapshot(
            DayOfMonth: dayOfMonth,
            // calendar.MonthName returns EnumMonth in VS 1.22 (was 'string' in 1.21.x).
            // Normalize to lowercase so viewmodels can build lang keys like "month-january"
            // without depending on the API enum type.
            MonthKey: calendar.MonthName.ToString().ToLowerInvariant(),
            Year: calendar.Year,
            Hour: hour,
            Minute: minute);
    }

    /// <inheritdoc />
    public EnumSeason GetSeason(BlockPos pos)
    {
        // We deliberately derive season from the calendar month rather than the
        // position-based IGameCalendar.GetSeason(pos) API.
        //
        // GetSeason routes through the OnGetHemisphere / OnGetLatitude delegates
        // that the survival mod assigns during world-gen init. Third-party
        // world-gen mods (confirmed: Realistic Biome Generation) override
        // polarEquatorDistance and patch GenMaps.getLatitude in ways that cause
        // GetSeason to return an incorrect hemisphere for the player's position —
        // players near the equator see "Fall" on May 1st.
        //
        // For a display mod, the calendar month is the authoritative answer to
        // "what time of year is it?" and is unaffected by latitude patches.
        // The `pos` parameter is accepted only to satisfy the interface; it is
        // intentionally unused here.
        int month = _api.World.Calendar.Month;
        return month switch
        {
            3 or 4 or 5 => EnumSeason.Spring,
            6 or 7 or 8 => EnumSeason.Summer,
            9 or 10 or 11 => EnumSeason.Fall,
            _ => EnumSeason.Winter,  // months 12, 1, 2
        };
    }
}
