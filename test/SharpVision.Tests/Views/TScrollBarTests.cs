using System.Linq;

using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Tests.Views;

public sealed class TScrollBarTests : CommandStateTest
{
    private static (TestScreen Screen, TScrollBar Bar, FillView Sibling) Setup(TRect bounds)
    {
        var screen = new TestScreen(20, 12);
        var sibling = new FillView(new TRect(15, 0, 20, 1), '.') { EventMask = evBroadcast };
        screen.Insert(sibling);
        var bar = new TScrollBar(bounds);
        screen.Insert(bar);
        bar.SetParams(0, 0, 100, 10, 1);
        sibling.Handled.Clear();
        return (screen, bar, sibling);
    }

    private static string Column(TestScreen screen, int x, int h) =>
        string.Concat(Enumerable.Range(0, h).Select(y => screen.Row(y)[x]));

    private static void Key(TView v, ushort keyCode)
    {
        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = keyCode } };
        v.HandleEvent(ref ev);
    }

    [Fact]
    public void Should_DrawArrowsPageAreaAndThumb()
    {
        var (screen, bar, _) = Setup(new TRect(0, 0, 1, 10));
        Assert.Equal("▲■▒▒▒▒▒▒▒▼", Column(screen, 0, 10));

        // (50 * (10 - 3) + 50) / 100 + 1 = 5.
        bar.SetValue(50);
        Assert.Equal(5, bar.GetPos());
        Assert.Equal("▲▒▒▒▒■▒▒▒▼", Column(screen, 0, 10));

        bar.SetRange(0, 0);
        Assert.Equal("▲▓▓▓▓▓▓▓▓▼", Column(screen, 0, 10));
    }

    [Fact]
    public void Should_ClampValue_And_BroadcastOnlyWhenItChanges()
    {
        var (_, bar, sibling) = Setup(new TRect(0, 0, 1, 10));

        bar.SetValue(500);
        Assert.Equal(100, bar.Value);
        Assert.Equal([cmScrollBarChanged], sibling.Handled);

        bar.SetValue(100);
        bar.SetRange(0, 200);
        Assert.Single(sibling.Handled);
    }

    [Fact]
    public void Should_StepWithKeys()
    {
        var (_, bar, sibling) = Setup(new TRect(0, 0, 1, 10));

        Key(bar, kbDown);
        Assert.Equal(1, bar.Value);
        Key(bar, kbPgDn);
        Assert.Equal(11, bar.Value);
        Key(bar, kbCtrlPgDn);
        Assert.Equal(100, bar.Value);
        Key(bar, kbCtrlE); // WordStar up.
        Assert.Equal(99, bar.Value);
        Assert.Contains(cmScrollBarClicked, sibling.Handled);

        var (_, h, _) = Setup(new TRect(0, 0, 10, 1));
        Key(h, kbCtrlRight);
        Assert.Equal(10, h.Value);
        Key(h, kbEnd);
        Assert.Equal(100, h.Value);
    }

    [Fact]
    public void Should_Step_When_ArrowClicked()
    {
        var (screen, bar, _) = Setup(new TRect(0, 0, 1, 10));
        screen.Events.Enqueue(new TEvent { What = evMouseUp, Mouse = new MouseEventType { Where = new TPoint(0, 9) } });

        var down = Ev.MouseDown(0, 9);
        bar.HandleEvent(ref down);

        Assert.Equal(1, bar.Value);
    }

    [Fact]
    public void Should_FollowMouse_When_ThumbOrPageAreaDragged()
    {
        var (screen, bar, _) = Setup(new TRect(0, 0, 1, 10));
        // Dragged to row 5: ((5 - 1) * 100 + 3) / 7 = 57. The page area jumps there too.
        screen.Events.Enqueue(new TEvent { What = evMouseMove, Mouse = new MouseEventType { Where = new TPoint(0, 5) } });
        screen.Events.Enqueue(new TEvent { What = evMouseUp, Mouse = new MouseEventType { Where = new TPoint(0, 5) } });

        var down = Ev.MouseDown(0, 1);
        bar.HandleEvent(ref down);

        Assert.Equal(57, bar.Value);
        Assert.Equal('■', screen.Row(5)[0]);
    }

    [Fact]
    public void Should_ScrollThreeArrowSteps_When_WheelTurned()
    {
        var (_, bar, _) = Setup(new TRect(0, 0, 1, 10));

        var ev = new TEvent { What = evMouseWheel, Mouse = new MouseEventType { Wheel = mwDown } };
        bar.HandleEvent(ref ev);

        Assert.Equal(3, bar.Value);
        Assert.Equal(evNothing, ev.What);
    }

    [Fact]
    public void Should_PlaceStandardScrollBarsOnTheFrame()
    {
        var w = new TWindow(new TRect(0, 0, 20, 10), "W", 0);

        var v = w.StandardScrollBar(sbVertical | sbHandleKeyboard);
        var h = w.StandardScrollBar(sbHorizontal);

        Assert.Equal(new TRect(19, 1, 20, 9), v.GetBounds());
        Assert.Equal(new TRect(2, 9, 18, 10), h.GetBounds());
        Assert.NotEqual(0, v.Options & ofPostProcess);
        Assert.Equal(0, h.Options & ofPostProcess);
    }
}
