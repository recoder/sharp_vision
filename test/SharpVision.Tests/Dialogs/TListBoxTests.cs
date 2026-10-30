using System.Collections.Generic;

using SharpVision.Dialogs;
using SharpVision.Objects;
using SharpVision.Views;
using Xunit;

namespace SharpVision.Tests.Dialogs;

public sealed class TListBoxTests : CommandStateTest
{
    private static (Views.TestScreen Screen, TListBox Box, TScrollBar Bar) Setup()
    {
        TView.ShowMarkers = false;
        var (screen, desk) = DialogDesk.Create();
        var d = new TDialog(new TRect(0, 0, 30, 8), null);
        var bar = new TScrollBar(new TRect(13, 1, 14, 4));
        var box = new TListBox(new TRect(1, 1, 13, 4), 1, bar);
        d.Insert(bar);
        d.Insert(box);
        desk.Insert(d);
        return (screen, box, bar);
    }

    [Fact]
    public void Should_ShowEmptyText_Until_ListSet()
    {
        var (screen, box, bar) = Setup();
        Assert.Equal(" <empty>", screen.Row(1)[1..9]);

        box.NewList(["apple", "banana", "cherry", "date"]);

        // One column is Size.X + 1 wide, so its divider falls just outside the view.
        Assert.Equal(" apple      ", screen.Row(1)[1..13]);
        Assert.Equal(" cherry     ", screen.Row(3)[1..13]);
        Assert.Equal((4, 0), (box.Range, box.Focused));
        Assert.Equal(3, bar.MaxVal);
    }

    [Fact]
    public void Should_TransferItemsAndSelection()
    {
        var (_, box, _) = Setup();
        List<string> items = ["a", "b", "c", "d", "e"];

        box.SetData(new object?[] { new TListBoxRec { Items = items, Selection = 4 } });
        var rec = new object?[1];
        box.GetData(rec);

        var r = Assert.IsType<TListBoxRec>(rec[0]);
        Assert.Same(items, r.Items);
        Assert.Equal(4, r.Selection);
        Assert.Equal(2, box.TopItem);
        Assert.Same(items, box.List());
    }

    [Fact]
    public void Should_TruncateText()
    {
        var (_, box, _) = Setup();
        box.NewList(["abcdef"]);

        Assert.Equal("abc", box.GetText(0, 3));
    }
}
