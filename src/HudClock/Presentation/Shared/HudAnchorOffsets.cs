using HudClock.Configuration;

namespace HudClock.Presentation.Shared;

/// <summary>
/// Fixed anchor offsets chosen to keep our HUDs clear of vanilla UI elements
/// (minimap, hotbar, hover tooltip, coordinates readout) at the default GUI
/// scale. Values are in logical pixels; Vintage Story applies
/// <c>RuntimeEnv.GUIScale</c> automatically when laying out, so these should
/// track correctly if the user changes GUI scale.
/// </summary>
/// <remarks>
/// These are hand-picked constants, tested only at the contributor's
/// resolution. Users on very different resolutions may see slightly different
/// spacing. Tune values below if you see overlap at your setup.
/// </remarks>
internal static class HudAnchorOffsets
{
    /// <summary>
    /// Offset for a HUD that sits alone in the given anchor corner. Chosen
    /// to clear vanilla UI that typically occupies each corner. Tweak here
    /// if you see overlap.
    /// </summary>
    /// <remarks>
    /// Sign convention: for <b>top</b> anchors, positive Y pushes the dialog
    /// down (away from the top edge, into the screen). For <b>bottom</b>
    /// anchors, <b>negative</b> Y pushes the dialog up (away from the bottom
    /// edge, into the screen). The wiki example reads:
    /// <c>.WithFixedAlignmentOffset(0, -10)</c> moves a bottom-aligned child
    /// up 10 pixels.
    /// </remarks>
    public static int GetSoloOffsetY(HudAnchor anchor) => anchor switch
    {
        // Vanilla-empty corner — flush to edge.
        HudAnchor.TopLeft => 0,
        HudAnchor.BottomLeft => 0,
        HudAnchor.BottomRight => 0,

        // Top-right typically hosts the minimap. 260 covers the default
        // minimap at GUI scale 1.0 with a comfortable gap below.
        HudAnchor.TopRight => 260,

        // Top-center gets the hover tooltip plus the claim banner when the
        // player is standing in a claim. 110 clears both.
        HudAnchor.TopCenter => 110,

        // Bottom-center hosts the hotbar. Negative to push the dialog upward
        // away from the bottom edge. -150 gives clearance above the hotbar.
        HudAnchor.BottomCenter => -150,

        _ => 0,
    };

    /// <summary>
    /// True when the anchor is one of the three bottom-edge anchors. Used
    /// when stacking sibling HUDs: for bottom anchors, "below Main" on
    /// screen means a more-negative offset, since offsets grow away from the
    /// anchor edge.
    /// </summary>
    public static bool IsBottomAnchor(HudAnchor anchor) => anchor switch
    {
        HudAnchor.BottomLeft or HudAnchor.BottomRight or HudAnchor.BottomCenter => true,
        _ => false,
    };

    /// <summary>
    /// Offset for the Claim banner (always top-center). Separate from the
    /// main/storm HUD top-center offset because the banner is a different
    /// shape and sits in a slightly different visual slot.
    /// </summary>
    public const int ClaimBannerOffsetY = 60;

    /// <summary>
    /// Vertical spacing adjustment between stacked HUDs in the same corner
    /// (e.g. between Main and Storm when both share an anchor). Tunes the
    /// visible gap beyond what the two dialogs' own internal paddings
    /// already produce. Negative values pull them closer; positive values
    /// push them apart.
    /// </summary>
    public const int InterHudPadding = -5;
}
