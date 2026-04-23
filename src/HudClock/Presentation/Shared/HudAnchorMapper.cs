using System;
using HudClock.Configuration;
using Vintagestory.API.Client;

namespace HudClock.Presentation.Shared;

/// <summary>
/// Maps the mod's <see cref="HudAnchor"/> configuration value to the
/// game's <see cref="EnumDialogArea"/>. Kept in the presentation layer so
/// the configuration layer never takes a dependency on VS types.
/// </summary>
internal static class HudAnchorMapper
{
    public static EnumDialogArea ToDialogArea(this HudAnchor anchor) => anchor switch
    {
        HudAnchor.TopLeft      => EnumDialogArea.LeftTop,
        HudAnchor.TopCenter    => EnumDialogArea.CenterTop,
        HudAnchor.TopRight     => EnumDialogArea.RightTop,
        HudAnchor.BottomLeft   => EnumDialogArea.LeftBottom,
        HudAnchor.BottomCenter => EnumDialogArea.CenterBottom,
        HudAnchor.BottomRight  => EnumDialogArea.RightBottom,
        _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null),
    };
}
