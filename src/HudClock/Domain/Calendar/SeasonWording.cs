using System;
using System.Globalization;

namespace HudClock.Domain.Calendar;

/// <summary>
/// Decides whether the English HUD says "Autumn" or "Fall".
/// </summary>
/// <remarks>
/// Vintage Story ships a single English locale ("en", no en-US/en-GB split),
/// so the game language can't answer this. The OS region can: North American
/// English says "Fall", everyone else says "Autumn". A visible settings row
/// for one word felt out of place, so the choice is automatic, with
/// <c>Appearance.UseAutumn</c> in the settings JSON as a manual override.
/// </remarks>
internal static class SeasonWording
{
    /// <summary>
    /// Resolve the effective choice. An explicit setting always wins; null
    /// means "auto" and defers to the region in <paramref name="cultureName"/>.
    /// </summary>
    public static bool UseAutumn(bool? setting, string cultureName)
        => setting ?? RegionPrefersAutumn(cultureName);

    /// <summary>
    /// True unless the region is one where "Fall" is the everyday word
    /// (United States, Canada). Unknown or empty regions return false so
    /// behavior matches every release before 4.5.0.
    /// </summary>
    public static bool RegionPrefersAutumn(string cultureName)
    {
        int dash = cultureName.LastIndexOf('-');
        if (dash < 0 || dash == cultureName.Length - 1) return false;

        string region = cultureName.Substring(dash + 1);
        return !region.Equals("US", StringComparison.OrdinalIgnoreCase)
            && !region.Equals("CA", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Best available culture name with a region part (e.g. "en-GB").
    /// </summary>
    /// <remarks>
    /// The game may run with an invariant <see cref="CultureInfo.CurrentCulture"/>
    /// to keep number parsing stable, which carries no region, so fall back
    /// through the UI culture to the OS install culture.
    /// </remarks>
    public static string DetectCultureName()
    {
        foreach (CultureInfo c in new[] { CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture, CultureInfo.InstalledUICulture })
        {
            if (c.Name.IndexOf('-') > 0) return c.Name;
        }
        return string.Empty;
    }
}
