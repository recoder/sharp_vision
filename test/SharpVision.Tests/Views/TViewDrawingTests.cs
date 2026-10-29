using SharpVision.Drawing;
using SharpVision.Objects;
using SharpVision.Views;
using Xunit;

using static SharpVision.Drawing.StyleFlags;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Tests.Views;

public sealed class TViewDrawingTests
{
    [Fact]
    public void Should_DrawIntoOwnerBuffer_When_Inserted()
    {
        var screen = new TestScreen(6, 2);

        screen.Insert(new FillView(new TRect(1, 0, 4, 2), 'a'));

        Assert.Equal(" aaa  ", screen.Row(0));
        Assert.Equal(" aaa  ", screen.Row(1));
    }

    [Fact]
    public void Should_NotOverwriteFrontView_When_BackViewRedraws()
    {
        var screen = new TestScreen(6, 1);
        var back = new FillView(new TRect(0, 0, 4, 1), 'a');
        var front = new FillView(new TRect(2, 0, 6, 1), 'b');
        screen.Insert(back);
        screen.Insert(front);

        back.DrawView();

        Assert.Equal("aabbbb", screen.Row(0));
    }

    [Fact]
    public void Should_RedrawViewsBehind_When_FrontViewHidden()
    {
        var screen = new TestScreen(6, 1);
        var back = new FillView(new TRect(0, 0, 6, 1), 'a');
        var front = new FillView(new TRect(2, 0, 4, 1), 'b');
        screen.Insert(back);
        screen.Insert(front);
        Assert.Equal("aabbaa", screen.Row(0));

        front.Hide();

        Assert.Equal("aaaaaa", screen.Row(0));
    }

    [Fact]
    public void Should_ReportExposure()
    {
        var screen = new TestScreen(6, 1);
        var back = new FillView(new TRect(0, 0, 3, 1), 'a');
        var partial = new FillView(new TRect(0, 0, 4, 1), 'p');
        screen.Insert(back);
        Assert.True(back.Exposed());

        screen.Insert(partial);
        Assert.False(back.Exposed());
        Assert.True(partial.Exposed());

        partial.Hide();
        Assert.True(back.Exposed());
        Assert.False(partial.Exposed());
    }

    [Fact]
    public void Should_ShadeCellsBehind_When_ViewHasShadow()
    {
        var screen = new TestScreen(6, 3);
        screen.Insert(new FillView(new TRect(0, 0, 6, 3), 'a', 0x1F));
        var front = new FillView(new TRect(1, 0, 3, 1), 'b');
        front.SetState(sfShadow, true);

        screen.Insert(front);

        Assert.Equal("abbaaa", screen.Row(0));
        Assert.Equal("aaaaaa", screen.Row(1));
        var shaded = screen.AttrAt(3, 1);
        Assert.Equal((byte)TView.ShadowAttr, (byte)new TColorAttr(shaded.Foreground, shaded.Background));
        Assert.True((shaded.Style & slWindowShadow) != 0);
        Assert.Equal(screen.AttrAt(4, 1), shaded);
        Assert.Equal(new TColorAttr(0x1F), screen.AttrAt(2, 1));
        Assert.Equal(new TColorAttr(0x1F), screen.AttrAt(5, 1));
    }

    [Fact]
    public void Should_ClipToOwnerExtent_When_ViewExceedsIt()
    {
        var screen = new TestScreen(4, 1);

        screen.Insert(new FillView(new TRect(2, 0, 10, 3), 'x'));

        Assert.Equal("  xx", screen.Row(0));
    }

    [Fact]
    public void Should_WriteThroughNestedGroups()
    {
        var screen = new TestScreen(6, 2);
        var group = new TGroup(new TRect(1, 1, 5, 2));
        group.Insert(new FillView(new TRect(1, 0, 3, 1), 'n'));

        screen.Insert(group);

        Assert.Equal("  nn  ", screen.Row(1));
    }

    [Fact]
    public void Should_WriteStringsAndCharsInPaletteColors()
    {
        var screen = new TestScreen(8, 1);
        var view = new FillView(new TRect(0, 0, 8, 1), ' ');
        screen.Insert(view);

        view.WriteStr(1, 0, "中ab", 0x1E);
        view.WriteChar(6, 0, '*', 0x1E, 5);

        // The wide character takes two cells; the five stars are clipped to the view.
        Assert.Equal(" 中ab **", screen.Row(0));
        Assert.Equal(new TColorAttr(0x1E), screen.AttrAt(1, 0));
        Assert.Equal(new TColorAttr(0x1E), screen.AttrAt(7, 0));
        Assert.Equal(new TColorAttr(0x07), screen.AttrAt(5, 0));
    }
}
