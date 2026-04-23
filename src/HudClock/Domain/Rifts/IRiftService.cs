namespace HudClock.Domain.Rifts;

/// <summary>
/// Read access to temporal rift weather from <c>ModSystemRiftWeather</c>.
/// </summary>
internal interface IRiftService
{
    /// <summary>True if the world allows rifts (<c>temporalRifts != "off"</c>) and the rift system loaded successfully.</summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Current rift-activity code (e.g. <c>"calm"</c>, <c>"swarm"</c>) used to
    /// look up a translated display string. Returns null if unavailable.
    /// </summary>
    string? GetCurrentActivityCode();
}
