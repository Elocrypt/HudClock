namespace HudClock.Configuration;

/// <summary>Visibility modes for the temporal storm dialog.</summary>
internal enum StormDisplay
{
    /// <summary>Always visible whenever storms are enabled in world config.</summary>
    Always,

    /// <summary>Only visible when a storm is approaching soon or currently active.</summary>
    TriggerOnly,

    /// <summary>Never shown.</summary>
    Hidden,
}

/// <summary>Options controlling the temporal storm dialog.</summary>
internal sealed class StormOptions
{
    public StormDisplay Display { get; set; } = StormDisplay.TriggerOnly;
}
