using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Tests.Dialogs;

public sealed class TClusterTests : CommandStateTest
{
    private sealed class ProbeCheckBoxes(TRect bounds, TSItem? items) : TCheckBoxes(bounds, items)
    {
        public uint Val => Value;

        public int Selected => Sel;
    }

    private static (Views.TestScreen Screen, T Cluster) Setup<T>(System.Func<TRect, TSItem, T> create)
        where T : TCluster
    {
        TView.ShowMarkers = false;
        var (screen, desk) = DialogDesk.Create();
        var d = new TDialog(new TRect(0, 0, 40, 12), null);
        var c = create(new TRect(2, 2, 30, 4), new TSItem("~O~ne", new TSItem("~T~wo", new TSItem("T~h~ree", null))));
        d.Insert(c);
        desk.Insert(d);
        return (screen, c);
    }

    private static void Key(TView v, ushort keyCode)
    {
        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = keyCode } };
        v.HandleEvent(ref ev);
    }

    private static ushort GetValue(TCluster c)
    {
        var rec = new object?[1];
        c.GetData(rec);
        return (ushort)rec[0]!;
    }

    [Fact]
    public void Should_DrawItemsInColumns_And_MarkPressedBox()
    {
        var (screen, c) = Setup((r, s) => new TCheckBoxes(r, s));

        Assert.Equal(" [ ] One  [ ] Three", screen.Row(2)[2..21]);
        Assert.Equal(" [ ] Two ", screen.Row(3)[2..11]);

        Key(c, ' ');
        Assert.Equal(" [X] One", screen.Row(2)[2..10]);
        Assert.Equal(1, GetValue(c));
    }

    [Fact]
    public void Should_ToggleBox_When_HotKeyPressed()
    {
        var (_, c) = Setup((r, s) => new TCheckBoxes(r, s));

        Key(c, kbAltH);
        Key(c, kbAltT);
        Key(c, kbAltH);

        Assert.Equal(0b010, GetValue(c));
    }

    [Fact]
    public void Should_MoveRadioValueWithSelection()
    {
        var (screen, c) = Setup((r, s) => new TRadioButtons(r, s));

        Key(c, kbDown);
        Assert.Equal(1, GetValue(c));
        Assert.Equal(" ( ) One  ( ) Three", screen.Row(2)[2..21]);
        Assert.Equal(" (•) Two", screen.Row(3)[2..10]);

        Key(c, kbRight);
        Assert.Equal(0, GetValue(c)); // Item 3 has no neighbour on the right, so it wraps to 0.

        c.SetData(new object?[] { (ushort)2 });
        Key(c, kbUp);
        Assert.Equal(1, GetValue(c));
    }

    [Fact]
    public void Should_SkipDisabledItems_And_BecomeUnselectable_When_AllDisabled()
    {
        var (_, c) = Setup((r, s) => new ProbeCheckBoxes(r, s));

        c.SetButtonState(0b010, false);
        Key(c, kbDown);
        Assert.Equal(2, c.Selected);

        c.SetButtonState(0b101, false);
        Assert.Equal(0, c.Options & ViewFlags.ofSelectable);
    }

    [Fact]
    public void Should_PressItem_When_ClickedAndReleasedOnIt()
    {
        var (screen, c) = Setup((r, s) => new ProbeCheckBoxes(r, s));
        var up = new TEvent { What = evMouseUp, Mouse = new MouseEventType { Where = new TPoint(4, 3) } };

        screen.Events.Enqueue(up);
        var down = Views.Ev.MouseDown(4, 3);
        c.HandleEvent(ref down);
        Assert.Equal(0b010u, c.Val);

        // Released over another item: nothing is pressed.
        screen.Events.Enqueue(new TEvent { What = evMouseUp, Mouse = new MouseEventType { Where = new TPoint(4, 2) } });
        down = Views.Ev.MouseDown(4, 3);
        c.HandleEvent(ref down);
        Assert.Equal(0b010u, c.Val);
        Assert.Equal(1, c.Selected);
    }
}
