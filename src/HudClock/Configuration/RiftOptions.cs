namespace HudClock.Configuration;

/// <summary>Visibility modes for the temporal rift activity line.</summary>
internal enum RiftDisplay
{
    /// <summary>Always shown, even if the world has rifts disabled.</summary>
    Always,

    /// <summary>Shown only if the world has rifts enabled (<c>temporalRifts != "off"</c>).</summary>
    WorldConfigDependent,

    /// <summary>Never shown.</summary>
    Hidden,
}

/// <summary>Options controlling the temporal rift line on the main HUD.</summary>
internal sealed class RiftOptions
{
    public RiftDisplay Display { get; set; } = RiftDisplay.WorldConfigDependent;
}
