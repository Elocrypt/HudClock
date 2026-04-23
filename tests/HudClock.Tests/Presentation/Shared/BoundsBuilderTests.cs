using HudClock.Presentation.Shared;
using Vintagestory.API.Client;

namespace HudClock.Tests.Presentation.Shared;

public class BoundsBuilderTests
{
    [Fact]
    public void First_line_starts_at_top_padding()
    {
        var b = new BoundsBuilder(offsetX: 10, lineHeight: 20, padding: 5);

        ElementBounds line = b.AddLine(width: 300);

        Assert.Equal(10, line.fixedX);
        Assert.Equal(5,  line.fixedY);
        Assert.Equal(300, line.fixedWidth);
        Assert.Equal(20,  line.fixedHeight);
    }

    [Fact]
    public void Subsequent_lines_stack_by_line_height()
    {
        var b = new BoundsBuilder(offsetX: 10, lineHeight: 20, padding: 5);

        b.AddLine(width: 300);
        ElementBounds second = b.AddLine(width: 300);
        ElementBounds third  = b.AddLine(width: 300);

        Assert.Equal(25, second.fixedY);  // 5 + 20
        Assert.Equal(45, third.fixedY);   // 5 + 40
    }

    [Fact]
    public void Lines_collection_reflects_insertion_order()
    {
        var b = new BoundsBuilder(offsetX: 0, lineHeight: 10, padding: 0);

        ElementBounds a = b.AddLine(width: 100);
        ElementBounds c = b.AddLine(width: 100);

        Assert.Equal(2, b.Lines.Count);
        Assert.Same(a, b.Lines[0]);
        Assert.Same(c, b.Lines[1]);
    }

    [Fact]
    public void TotalHeight_includes_top_and_bottom_padding()
    {
        var b = new BoundsBuilder(offsetX: 0, lineHeight: 20, padding: 5);

        b.AddLine(width: 100);
        b.AddLine(width: 100);

        // padding(5) + 2 * lineHeight(20) + padding(5) = 50.
        Assert.Equal(50, b.TotalHeight);
    }

    [Fact]
    public void AddRightSlot_positions_square_at_right_edge()
    {
        var b = new BoundsBuilder(offsetX: 10, lineHeight: 20, padding: 5);
        b.AddLine(width: 300);
        b.AddLine(width: 300);

        ElementBounds slot = b.AddRightSlot(lineWidth: 300, size: 100);

        // X: offsetX(10) + lineWidth(300) - size(100) = 210.
        Assert.Equal(210, slot.fixedX);
        Assert.Equal(100, slot.fixedWidth);
        // Slot spans the full stack height including padding.
        Assert.Equal(b.TotalHeight, slot.fixedHeight);
    }
}
