using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.StdDlg;
using SharpVision.Tests.Dialogs;
using SharpVision.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Tests.StdDlg;

public sealed class TSortedListBoxTests : CommandStateTest
{
    private static TSortedListBox Setup(params string[] items)
    {
        TView.ShowMarkers = false;
        var (_, desk) = DialogDesk.Create();
        var d = new TDialog(new TRect(0, 0, 30, 8), null);
        var box = new TSortedListBox(new TRect(1, 1, 13, 6), 1, null);
        d.Insert(box);
        desk.Insert(d);
        box.NewList(items);
        return box;
    }

    private static TEvent Key(TView v, ushort keyCode)
    {
        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = keyCode } };
        v.HandleEvent(ref ev);
        return ev;
    }

    [Fact]
    public void Should_JumpToPrefix_And_MoveCursorWithTypedText()
    {
        var box = Setup("Apple", "Apricot", "Banana", "Blueberry", "Cherry");

        Assert.Equal(evNothing, Key(box, 'B').What);
        Assert.Equal((2, 2), (box.Focused, box.Cursor.X));
        Key(box, 'l');
        Assert.Equal((3, 3), (box.Focused, box.Cursor.X));

        // Backspace searches for the shorter prefix again, which goes back to Banana.
        Key(box, kbBack);
        Assert.Equal((2, 2), (box.Focused, box.Cursor.X));
    }

    [Fact]
    public void Should_KeepFocus_When_NoItemMatches()
    {
        var box = Setup("Apple", "Banana");

        var ev = Key(box, 'Z');

        Assert.Equal(0, box.Focused);
        // Letters are eaten even when nothing matches; other characters pass on.
        Assert.Equal(evNothing, ev.What);
        Assert.Equal(evKeyDown, Key(box, '7').What);
    }

    [Fact]
    public void Should_NotFindCapitalizedItems_When_TypingLowercase()
    {
        // UPSTREAM: the search uses the collection's case-sensitive order (strcmp), so 'b' sorts
        // after every capitalized item, even though the prefix check ignores case.
        var box = Setup("Apple", "Banana");

        Key(box, 'b');

        Assert.Equal(0, box.Focused);
    }

    [Fact]
    public void Should_JumpToDot_When_DotTyped()
    {
        var box = Setup("readme.txt", "setup.cfg", "setup.exe");

        Key(box, 's');
        Key(box, '.');
        Key(box, 'e');

        Assert.Equal(2, box.Focused);
    }
}
