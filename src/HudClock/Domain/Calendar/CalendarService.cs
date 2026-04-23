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
        if (pos is null) throw new ArgumentNullException(nameof(pos));
        return _api.World.Calendar.GetSeason(pos);
    }
}
