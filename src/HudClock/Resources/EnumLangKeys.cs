using System;
using HudClock.Configuration;

namespace HudClock.Resources;

/// <summary>
/// Explicit enum → lang-key-suffix mappings. Using a switch expression rather
/// than <c>Enum.ToString().ToLowerInvariant()</c> insulates lang files from
/// C# enum renames: if a future refactor renames <c>TriggerOnly</c> to
/// <c>WhenApproaching</c>, the lang key stays <c>"triggeronly"</c> until the
/// mapping here is deliberately updated.
/// </summary>
internal static class EnumLangKeys
{
    public static string ToKeySuffix(this HudAnchor value) => value switch
    {
        HudAnchor.TopLeft      => "topleft",
        HudAnchor.TopCenter    => "topcenter",
        HudAnchor.TopRight     => "topright",
        HudAnchor.BottomLeft   => "bottomleft",
        HudAnchor.BottomCenter => "bottomcenter",
        HudAnchor.BottomRight  => "bottomright",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static string ToKeySuffix(this TimeFormat value) => value switch
    {
        TimeFormat.TwelveHour     => "twelvehour",
        TimeFormat.TwentyFourHour => "twentyfourhour",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static string ToKeySuffix(this WindDisplay value) => value switch
    {
        WindDisplay.BeaufortText => "beauforttext",
        WindDisplay.Percentage   => "percentage",
        WindDisplay.Hidden       => "hidden",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static string ToKeySuffix(this StormDisplay value) => value switch
    {
        StormDisplay.Always      => "always",
        StormDisplay.TriggerOnly => "triggeronly",
        StormDisplay.Hidden      => "hidden",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static string ToKeySuffix(this RiftDisplay value) => value switch
    {
        RiftDisplay.Always               => "always",
        RiftDisplay.WorldConfigDependent => "worldconfigdependent",
        RiftDisplay.Hidden               => "hidden",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };
}
