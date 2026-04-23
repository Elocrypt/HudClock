namespace HudClock.Domain.Storms;

/// <summary>Read access to temporal storm state from <c>SystemTemporalStability</c>.</summary>
internal interface IStormService
{
    /// <summary>
    /// Fetch the current storm status. Returns <see cref="StormStatus.Unavailable"/>
    /// if the storm system can't be reached (game internals renamed, etc.).
    /// </summary>
    StormStatus GetStatus();
}
