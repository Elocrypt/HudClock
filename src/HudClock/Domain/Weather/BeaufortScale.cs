namespace HudClock.Domain.Weather;

/// <summary>
/// Maps a normalized wind-speed value (0..~1.2) to a Beaufort scale number 0-12.
/// Thresholds reproduce the 3.x behavior exactly but are expressed as a lookup
/// table rather than an if-else chain.
/// </summary>
internal static class BeaufortScale
{
    // Thresholds[i] is the minimum wind speed for Beaufort level (i + 1).
    // Anything below Thresholds[0] is Beaufort 0.
    private static readonly double[] Thresholds =
    {
        0.02, // >= 0.02  -> 1
        0.06, // >= 0.06  -> 2
        0.12, // >= 0.12  -> 3
        0.20, // >= 0.20  -> 4
        0.30, // >= 0.30  -> 5
        0.40, // >= 0.40  -> 6
        0.51, // >= 0.51  -> 7
        0.62, // >= 0.62  -> 8
        0.75, // >= 0.75  -> 9
        0.88, // >= 0.88  -> 10
        1.02, // >= 1.02  -> 11
        1.18, // >= 1.18  -> 12
    };

    /// <summary>Convert a wind speed to its Beaufort level (0-12).</summary>
    public static int Level(double windSpeed)
    {
        int level = 0;
        for (int i = 0; i < Thresholds.Length; i++)
        {
            if (windSpeed >= Thresholds[i]) level = i + 1;
            else break;
        }
        return level;
    }
}
