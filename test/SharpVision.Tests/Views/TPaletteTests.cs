using SharpVision.Drawing;
using SharpVision.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Tests.Views;

public sealed class TPaletteTests
{
    [Fact]
    public void Should_StoreLengthInEntryZero()
    {
        var p = new TPalette([0x01, 0x02, 0x9F]);

        Assert.Equal(3, p.Length);
        Assert.Equal(new TColorAttr(0x01), p[1]);
        Assert.Equal(new TColorAttr(0x9F), p[3]);
    }

    [Fact]
    public void Should_AllowWritingEntries()
    {
        var p = new TPalette([0x01]);
        p[1] = 0x1F;

        Assert.Equal(new TColorAttr(0x1F), p[1]);
    }

    [Fact]
    public void Should_NotShareEntries_When_Copied()
    {
        var p = new TPalette([0x01, 0x02]);
        var copy = new TPalette(p);
        copy[1] = 0x70;

        Assert.Equal(new TColorAttr(0x01), p[1]);
    }

    [Fact]
    public void Should_ResizeAndCopy_When_Assigned()
    {
        var p = new TPalette([0x01]);
        var colors = new TColorAttr[] { new(TColor.FromRGB(0x112233), default), 0x07 };

        p.Assign(TPalette.FromAttributes(colors));

        Assert.Equal(2, p.Length);
        Assert.Equal(colors[0], p[1]);
        Assert.Equal(colors[1], p[2]);
    }

    [Fact]
    public void Should_ExcludeWheel_FromPositionalEvents()
    {
        Assert.Equal(evMouseDown | evMouseUp | evMouseMove | evMouseAuto, positionalEvents);
        Assert.Equal(evKeyDown | evCommand, focusedEvents);
    }
}
