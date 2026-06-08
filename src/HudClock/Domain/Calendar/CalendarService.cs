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
            Minute: minute,
            MoonPhaseKey: MoonPhaseKey(calendar.MoonPhase));
    }

    /// <summary>
    /// Map VS's <see cref="EnumMoonPhase"/> to a stable, astronomical lang-key
    /// suffix. The enum's own member names are engine-internal
    /// (<c>Empty/Grow1/Grow2/Grow3/Full/Shrink1/Shrink2/Shrink3</c>, integer
    /// values 0..7), so we translate to the conventional 8-phase names players
    /// recognize. <c>Full</c> is value 4 — which resolves the common
    /// "is full moon phase 4 or 5?" confusion, since some references 1-index
    /// the cycle. Kept as a string so <see cref="CalendarSnapshot"/> stays free
    /// of any VS API type, mirroring the MonthName → MonthKey treatment.
    /// </summary>
    private static string MoonPhaseKey(EnumMoonPhase phase) => phase switch
    {
        EnumMoonPhase.Empty   => "newmoon",
        EnumMoonPhase.Grow1   => "waxingcrescent",
        EnumMoonPhase.Grow2   => "firstquarter",
        EnumMoonPhase.Grow3   => "waxinggibbous",
        EnumMoonPhase.Full    => "fullmoon",
        EnumMoonPhase.Shrink1 => "waninggibbous",
        EnumMoonPhase.Shrink2 => "thirdquarter",
        EnumMoonPhase.Shrink3 => "waningcrescent",
        _ => "newmoon",
    };

    /// <inheritdoc />
    public EnumSeason GetSeason(BlockPos pos)
    {
        // Deliberately derives from the calendar month rather than the position-based
        // IGameCalendar.GetSeason API. GetSeason routes through OnGetHemisphere /
        // OnGetLatitude delegates that world-gen mods (confirmed: Realistic Biome
        // Generation) override, causing incorrect hemisphere detection. For a display
        // mod the month is the authoritative answer to "what time of year is it?" and
        // is completely unaffected by latitude patches.
        //
        // The `pos` parameter is accepted only to satisfy the interface; it is
        // intentionally unused here. Hemisphere-aware behavior is available via
        // GetSeasonAt when the user has explicitly opted in.
        int month = _api.World.Calendar.Month;
        return month switch
        {
            3 or 4 or 5 => EnumSeason.Spring,
            6 or 7 or 8 => EnumSeason.Summer,
            9 or 10 or 11 => EnumSeason.Fall,
            _ => EnumSeason.Winter,  // months 12, 1, 2
        };
    }

    /// <inheritdoc />
    public EnumSeason GetSeasonAt(BlockPos pos)
    {
        if (pos is null) throw new ArgumentNullException(nameof(pos));
        // Direct VS API call — hemisphere-aware but sensitive to world-gen mod
        // overrides of the latitude/hemisphere delegates.
        return _api.World.Calendar.GetSeason(pos);
    }
}
