using SharpVision.Dialogs;
using SharpVision.Drawing;
using SharpVision.Objects;
using SharpVision.Tests.Dialogs;
using SharpVision.Views;
using Xunit;

namespace SharpVision.Tests.Views;

public sealed class TScrollerTests : CommandStateTest
{
    // Draws each row as its content row number and column offset, shifted by Delta.
    private sealed class Rows(TRect bounds, TScrollBar? h, TScrollBar? v) : TScroller(bounds, h, v)
    {
        public int DrawCount;

        public override void Draw()
        {
            DrawCount++;
            var b = new TDrawBuffer();
            for (var y = 0; y < Size.Y; y++)
            {
                b.MoveChar(0, ' ', GetColor(1), Size.X);
                b.MoveStr(0, $"r{y + Delta.Y}c{Delta.X}", GetColor(1));
                WriteLine(0, y, Size.X, 1, b);
            }
        }
    }

    private static (TestScreen Screen, Rows Scroller, TScrollBar H, TScrollBar V) Setup()
    {
        var (screen, desk) = DialogDesk.Create();
        var d = new TDialog(new TRect(0, 0, 30, 10), null);
        var h = new TScrollBar(new TRect(1, 6, 11, 7));
        var v = new TScrollBar(new TRect(11, 1, 12, 6));
        var s = new Rows(new TRect(1, 1, 11, 6), h, v);
        d.Insert(h);
        d.Insert(v);
        d.Insert(s);
        desk.Insert(d);
        return (screen, s, h, v);
    }

    [Fact]
    public void Should_SetScrollBarRanges_When_LimitSet()
    {
        var (_, s, h, v) = Setup();

        s.SetLimit(40, 20);

        // Max is limit - size, page step is size - 1.
        Assert.Equal((0, 30, 9), (h.MinVal, h.MaxVal, h.PgStep));
        Assert.Equal((0, 15, 4), (v.MinVal, v.MaxVal, v.PgStep));
    }

    [Fact]
    public void Should_FollowScrollBar_And_MoveCursor()
    {
        var (screen, s, _, v) = Setup();
        s.SetLimit(40, 20);
        s.SetCursor(2, 3);

        v.SetValue(2);

        Assert.Equal(new TPoint(0, 2), s.Delta);
        Assert.Equal(new TPoint(2, 1), s.Cursor);
        Assert.Equal("r2c0", screen.Row(1)[1..5]);
    }

    [Fact]
    public void Should_RedrawOnce_When_ScrolledToBothAxes()
    {
        var (screen, s, _, _) = Setup();
        s.SetLimit(40, 20);
        var before = s.DrawCount;

        s.ScrollTo(5, 3);

        Assert.Equal(new TPoint(5, 3), s.Delta);
        Assert.Equal(before + 1, s.DrawCount);
        Assert.Equal("r3c5", screen.Row(1)[1..5]);
    }
}
