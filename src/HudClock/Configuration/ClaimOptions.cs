namespace HudClock.Configuration;

/// <summary>Options controlling the land-claim indicator.</summary>
internal sealed class ClaimOptions
{
    /// <summary>When true, display a banner naming the claim owner while the player stands inside a claimed block.</summary>
    public bool ShowClaimedArea { get; set; }
}
