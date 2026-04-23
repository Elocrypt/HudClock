using System.Collections.Generic;
using Vintagestory.API.Client;

namespace HudClock.Presentation.Shared;

/// <summary>
/// Fluent helper for building a vertical stack of <see cref="ElementBounds"/>
/// for a HUD panel. Replaces the per-dialog offset-tracking counters and
/// manual arithmetic in 3.x (<c>GetNextElementHeight</c>, <c>elementBounds.Add(...)</c>).
/// </summary>
/// <remarks>
/// Typical use inside a view's layout setup:
/// <code>
/// var layout = new BoundsBuilder(offsetX: 10, lineHeight: 17, padding: 5);
/// ElementBounds seasonLine = layout.AddLine(width: 300);
/// ElementBounds timeLine   = layout.AddLine(width: 300);
/// ElementBounds iconSlot   = layout.AddRightSlot(size: 100);
/// </code>
/// </remarks>
internal sealed class BoundsBuilder
{
    private readonly int _offsetX;
    private readonly int _lineHeight;
    private readonly int _padding;
    private readonly List<ElementBounds> _lines = new();
    private int _nextY;

    /// <summary>Create a builder configured with a common left offset and line height.</summary>
    /// <param name="offsetX">Left-edge X offset applied to every line.</param>
    /// <param name="lineHeight">Height of a single stacked line, in pixels.</param>
    /// <param name="padding">Top padding applied before the first line.</param>
    public BoundsBuilder(int offsetX, int lineHeight, int padding)
    {
        _offsetX = offsetX;
        _lineHeight = lineHeight;
        _padding = padding;
        _nextY = padding;
    }

    /// <summary>Append a single full-width stacked line and return its bounds.</summary>
    public ElementBounds AddLine(int width)
    {
        ElementBounds bounds = ElementBounds.Fixed(_offsetX, _nextY, width, _lineHeight);
        _lines.Add(bounds);
        _nextY += _lineHeight;
        return bounds;
    }

    /// <summary>
    /// Add a right-aligned slot (e.g. the season-icon region) that spans the
    /// full vertical stack of lines added so far. Typically called last.
    /// </summary>
    /// <param name="lineWidth">The line width used for <see cref="AddLine"/>.</param>
    /// <param name="size">Width and height of the square slot, in pixels.</param>
    public ElementBounds AddRightSlot(int lineWidth, int size)
    {
        int x = _offsetX + lineWidth - size;
        ElementBounds bounds = ElementBounds.Fixed(x, 0, size, TotalHeight);
        _lines.Add(bounds);
        return bounds;
    }

    /// <summary>All line bounds accumulated so far, in insertion order.</summary>
    public IReadOnlyList<ElementBounds> Lines => _lines;

    /// <summary>Total vertical height occupied by the accumulated lines, including padding.</summary>
    public int TotalHeight => _nextY + _padding;
}
