using SharpVision.ColorSel;
using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Tests.Dialogs;
using SharpVision.Tests.Views;
using SharpVision.Views;
using Xunit;

using static SharpVision.ColorSel.ColorSelCommands;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Tests.ColorSel;

public sealed class TColorSelTests : CommandStateTest
{
    private static TColorGroup Groups() =>
        new TColorGroup("A") + new TColorItem("a1", 1) + new TColorItem("a2", 2) +
        new TColorGroup("B") + new TColorItem("b1", 3);

    private static void Key(TView v, ushort keyCode)
    {
        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = keyCode } };
        v.HandleEvent(ref ev);
    }

    [Fact]
    public void Should_ChainItemsIntoTheLastGroup_When_Added()
    {
        var g = Groups();

        Assert.Equal(("A", "a1", "a2"), (g.Name, g.Items!.Name, g.Items.Next!.Name));
        Assert.Null(g.Items.Next.Next);
        Assert.Equal(("B", "b1", (byte)3), (g.Next!.Name, g.Next.Items!.Name, g.Next.Items.Index));
    }

    [Fact]
    public void Should_MarkSelectedColor_And_WrapWithArrowKeys()
    {
        var (screen, desk) = DialogDesk.Create(12, 4);
        var sel = new TColorSelector(new TRect(0, 0, 12, 4), TColorSelector.ColorSel.csForeground);
        desk.Insert(sel);

        Assert.Equal("█◘████████████", screen.Row(0)[..12] + "██");
        Assert.Equal(0x70, (byte)screen.AttrAt(1, 0));

        Key(sel, kbLeft); // 0 wraps to 15
        Assert.Equal("██████████◘█", screen.Row(3));
        Assert.Equal(15, (byte)screen.AttrAt(10, 3));
        Key(sel, kbDown); // 15 wraps to 0
        Key(sel, kbUp); // 0 wraps to 15
        Key(sel, kbUp);
        Assert.Equal("██████████◘█", screen.Row(2));

        var set = new TEvent { What = evBroadcast };
        set.Message.Command = cmColorSet;
        set.Message.InfoByte = 0x35;
        sel.HandleEvent(ref set);
        Assert.Equal("████◘███████", screen.Row(1));
    }

    [Fact]
    public void Should_EditPaletteCopy_And_RememberFocusedItems()
    {
        var (_, desk) = DialogDesk.Create(61, 18);
        var original = new TPalette([0x1F, 0x2E, 0x30]);
        var d = new TColorDialog(original, Groups());
        desk.Insert(d);
        var items = (TColorItemList)d.FirstThat(v => v is TColorItemList)!;
        // The background selector, inserted after the foreground one.
        var back = (TColorSelector)d.FirstThat(v => v is TColorSelector)!;

        Key(back, kbLeft); // background 1 -> 0 of entry 1
        items.FocusItem(1);
        Key(back, kbRight); // background 2 -> 3 of entry 2

        object?[] rec = [null];
        d.GetData(rec);
        var result = (TPalette)rec[0]!;
        Assert.Equal((0x0F, 0x3E, 0x30), ((byte)result[1], (byte)result[2], (byte)result[3]));
        Assert.Equal(0x1F, (byte)original[1]);

        var again = new TColorDialog(original, Groups());
        var againItems = (TColorItemList)again.FirstThat(v => v is TColorItemList)!;
        Assert.Equal(1, againItems.Focused);
    }
}
