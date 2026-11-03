using System.Linq;

using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Outline;
using SharpVision.Tests.Dialogs;
using SharpVision.Tests.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Tests.Outline;

public sealed class TOutlineTests : CommandStateTest
{
    private sealed class SelectingOutline(TRect bounds, TNode root) : TOutline(bounds, null, null, root)
    {
        public int Picked = -1;

        public TPoint Lim => Limit;

        public override void Selected(int i) => Picked = i;
    }

    // A ─ B ─ D
    //   └ C
    // E
    private static TNode Tree() =>
        new("A", new TNode("B", new TNode("D"), new TNode("C")), new TNode("E"));

    private static (TestScreen Screen, SelectingOutline Outline) Open()
    {
        var (screen, desk) = DialogDesk.Create(16, 6);
        var o = new SelectingOutline(new TRect(0, 0, 16, 6), Tree());
        desk.Insert(o);
        return (screen, o);
    }

    private static string[] Rows(TestScreen screen) =>
        Enumerable.Range(0, 6).Select(y => screen.Row(y).TrimEnd()).ToArray();

    private static void Key(TOutline o, ushort keyCode, char ch = '\0')
    {
        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = keyCode } };
        if (ch != '\0')
            ev.KeyDown.CharScan.CharCode = (byte)ch;
        o.HandleEvent(ref ev);
    }

    [Fact]
    public void Should_DrawGraph_With_LevelLines()
    {
        var (screen, o) = Open();

        Assert.Equal(["├──A", "│  ├──B", "│  │  └──D", "│  └──C", "└──E", ""], Rows(screen));
        Assert.Equal((5, 10), (o.Lim.Y, o.Lim.X));
        Assert.Equal(("├──A", 7), (screen.Row(0)[..4], (int)screen.AttrAt(0, 0)));
    }

    [Fact]
    public void Should_CollapseExpandAndMove_With_Keys()
    {
        var (screen, o) = Open();

        Key(o, kbNoKey, '-');
        Assert.Equal(["├─+A", "└──E", "", "", "", ""], Rows(screen));
        Key(o, kbDown);
        Key(o, kbDown);
        Assert.Equal(1, o.Foc);
        // Unfocused, collapsed text uses the 'not expanded' color; the graph stays normal.
        Assert.Equal((6, 8), ((int)screen.AttrAt(0, 0), (int)screen.AttrAt(3, 0)));

        Key(o, kbUp);
        Key(o, kbNoKey, '*');
        Assert.Equal(5, o.Lim.Y);

        Key(o, kbCtrlPgDn);
        Key(o, kbEnter);
        Assert.Equal((4, 4), (o.Foc, o.Picked));
    }

    [Fact]
    public void Should_Toggle_When_GraphIsClicked()
    {
        var (screen, o) = Open();

        screen.Events.Enqueue(new TEvent { What = evMouseUp, Mouse = new MouseEventType { Where = new TPoint(4, 1) } });
        var down = Ev.MouseDown(4, 1);
        o.HandleEvent(ref down);
        Assert.Equal(["├──A", "│  ├─+B", "│  └──C", "└──E", "", ""], Rows(screen));

        // A click on the text only focuses.
        screen.Events.Enqueue(new TEvent { What = evMouseUp, Mouse = new MouseEventType { Where = new TPoint(4, 2) } });
        down = Ev.MouseDown(7, 2);
        o.HandleEvent(ref down);
        Assert.Equal((2, 4), (o.Foc, o.Lim.Y));

        // UPSTREAM: the double-click flag is checked on the mouse-up event, so it never selects.
        screen.Events.Enqueue(new TEvent { What = evMouseUp, Mouse = new MouseEventType { Where = new TPoint(7, 2) } });
        down = Ev.MouseDown(7, 2);
        down.Mouse.EventFlags = meDoubleClick;
        o.HandleEvent(ref down);
        Assert.Equal(-1, o.Picked);
    }
}
