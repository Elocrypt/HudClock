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

    /// <summary>
    /// Screen corner the storm HUD anchors to. Separate from the main HUD's anchor so
    /// players can park the two dialogs in different corners.
    /// </summary>
    /// <remarks>
    /// Existing settings files predating this field deserialize with the default
    /// (<see cref="HudAnchor.TopLeft"/>), which preserves 3.x behavior.
    /// </remarks>
    public HudAnchor Anchor { get; set; } = HudAnchor.TopLeft;
}
