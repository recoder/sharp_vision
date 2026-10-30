using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Tests.Dialogs;
using SharpVision.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;

namespace SharpVision.Tests.Views;

public sealed class TListViewerTests : CommandStateTest
{
    private sealed class Items(TRect bounds, ushort cols, TScrollBar? v) : TListViewer(bounds, cols, null, v)
    {
        public override string GetText(int item, int maxLen) => $"Item {item}";
    }

    private static (TestScreen Screen, Items List, TScrollBar Bar, FillView Sibling) Setup(ushort cols = 1, int range = 10)
    {
        TView.ShowMarkers = false;
        var (screen, desk) = DialogDesk.Create();
        var d = new TDialog(new TRect(0, 0, 30, 8), null);
        var bar = new TScrollBar(new TRect(23, 1, 24, 4));
        var list = new Items(new TRect(1, 1, 23, 4), cols, bar);
        var sibling = new FillView(new TRect(25, 1, 26, 2), '.') { EventMask = evBroadcast };
        d.Insert(sibling);
        d.Insert(bar);
        d.Insert(list);
        // UPSTREAM: with a scroll bar, SetRange redraws only the bar, so set it before showing the list.
        list.SetRange(range);
        desk.Insert(d);
        sibling.Handled.Clear();
        return (screen, list, bar, sibling);
    }

    private static void Key(TView v, ushort keyCode)
    {
        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = keyCode } };
        v.HandleEvent(ref ev);
    }

    [Fact]
    public void Should_DrawItemsInColumnsWithDividers()
    {
        var (screen, _, _, _) = Setup(cols: 2);

        // 22 columns / 2 + 1 = 12 per column, the last one a divider (clipped in the last column).
        Assert.Equal(" Item 0    │ Item 3   ", screen.Row(1)[1..23]);
        Assert.Equal(" Item 2    │ Item 5   ", screen.Row(3)[1..23]);
    }

    [Fact]
    public void Should_ShowEmptyText_When_NoItems()
    {
        var (screen, _, _, _) = Setup(range: 0);

        Assert.Equal(" <empty>", screen.Row(1)[1..9]);
    }

    [Fact]
    public void Should_MoveFocusWithKeys_And_ScrollIntoView()
    {
        var (screen, list, bar, _) = Setup();

        Key(list, kbDown);
        Key(list, kbDown);
        Key(list, kbDown);
        Assert.Equal((3, 1), (list.Focused, list.TopItem));
        Assert.Equal(3, bar.Value);
        Assert.Equal(" Item 1", screen.Row(1)[1..8]);

        Key(list, kbCtrlPgDn);
        Assert.Equal((9, 7), (list.Focused, list.TopItem));
        Key(list, kbHome);
        Assert.Equal(7, list.Focused);
        Key(list, kbCtrlPgUp);
        Assert.Equal((0, 0), (list.Focused, list.TopItem));
    }

    [Fact]
    public void Should_FollowScrollBar()
    {
        var (_, list, bar, _) = Setup();

        bar.SetValue(5);

        Assert.Equal((5, 3), (list.Focused, list.TopItem));
    }

    [Fact]
    public void Should_FocusClickedItem_And_SelectOnDoubleClickOrSpace()
    {
        var (screen, list, _, sibling) = Setup(cols: 2);
        screen.Events.Enqueue(new TEvent { What = evMouseUp, Mouse = new MouseEventType { Where = new TPoint(14, 2) } });

        var down = Ev.MouseDown(14, 2);
        list.HandleEvent(ref down);
        Assert.Equal(4, list.Focused);
        Assert.Equal([cmScrollBarChanged], sibling.Handled);
        sibling.Handled.Clear();

        down = Ev.MouseDown(14, 2);
        down.Mouse.EventFlags = meDoubleClick;
        list.HandleEvent(ref down);
        Assert.Equal([cmListItemSelected], sibling.Handled);

        Key(list, ' ');
        Assert.Equal([cmListItemSelected, cmListItemSelected], sibling.Handled);
    }
}
