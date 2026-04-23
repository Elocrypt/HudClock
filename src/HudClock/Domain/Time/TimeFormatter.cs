using System;
using HudClock.Configuration;

namespace HudClock.Domain.Time;

/// <summary>
/// <see cref="ITimeFormatter"/> implementation that selects 12-hour or 24-hour
/// rendering based on <see cref="TimeOptions.Format"/>. The AM/PM translator is
/// injected so the formatter is pure and unit-testable — production code passes
/// <c>Vintagestory.API.Config.Lang.Get</c>.
/// </summary>
internal sealed class TimeFormatter : ITimeFormatter
{
    /// <summary>Lang key used when the formatted hour is before noon.</summary>
    public const string AmKey = "hudclock:am";

    /// <summary>Lang key used when the formatted hour is noon or later.</summary>
    public const string PmKey = "hudclock:pm";

    private readonly TimeOptions _options;
    private readonly Func<string, string> _translate;

    public TimeFormatter(TimeOptions options, Func<string, string> translate)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _translate = translate ?? throw new ArgumentNullException(nameof(translate));
    }

    /// <inheritdoc />
    public string Format(int hour24, int minute)
    {
        // Normalize defensively. Callers using calendar.HourOfDay at day boundaries
        // can legitimately hand us hour==24 on a floor, and bugged inputs shouldn't crash.
        int h = ((hour24 % 24) + 24) % 24;
        int m = ((minute % 60) + 60) % 60;

        return _options.Format == TimeFormat.TwentyFourHour
            ? $"{h:00}:{m:00}"
            : FormatTwelveHour(h, m);
    }

    private string FormatTwelveHour(int hour24, int minute)
    {
        // Standard 12h: 00:xx -> 12 AM, 01-11:xx -> 1-11 AM, 12:xx -> 12 PM, 13-23:xx -> 1-11 PM.
        int displayHour = hour24 % 12;
        if (displayHour == 0) displayHour = 12;
        string suffix = _translate(hour24 < 12 ? AmKey : PmKey);
        return $"{displayHour:00}:{minute:00} {suffix}";
    }
}
