namespace HudClock.Configuration;

/// <summary>Position of the main HUD panel on the screen.</summary>
internal enum HudAnchor
{
    TopLeft,
    TopCenter,
    TopRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
}

/// <summary>Options affecting where the HUD is drawn.</summary>
internal sealed class DisplayOptions
{
    public HudAnchor Anchor { get; set; } = HudAnchor.TopLeft;
}
