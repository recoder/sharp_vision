using System;

using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;
using Xunit;

using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Dialogs.HistList;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;

namespace SharpVision.Tests.Dialogs;

// HistList is global, so its tests and the THistory tests share one class.
public sealed class THistoryTests : CommandStateTest, IDisposable
{
    private readonly ushort _historySize = HistorySize;

    public THistoryTests() => ClearHistory();

    public new void Dispose()
    {
        HistorySize = _historySize;
        ClearHistory();
        base.Dispose();
    }

    private static TEvent Key(ushort keyCode) => new() { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = keyCode } };

    private static (Views.TestScreen Screen, TInputLine Line, THistory History) Setup()
    {
        var (screen, desk) = DialogDesk.Create();
        var d = new TDialog(new TRect(0, 0, 40, 15), null);
        var line = new TInputLine(new TRect(2, 2, 20, 3), 4);
        var history = new THistory(new TRect(20, 2, 23, 3), line, 1);
        d.Insert(line);
        d.Insert(history);
        desk.Insert(d);
        return (screen, line, history);
    }

    [Fact]
    public void Should_KeepStringsPerIdOldestFirst_And_MoveReaddedToNewest()
    {
        HistoryAdd(1, "one");
        HistoryAdd(1, "two");
        HistoryAdd(2, "other");
        HistoryAdd(1, "");
        HistoryAdd(1, "one");

        Assert.Equal(2, HistoryCount(1));
        Assert.Equal("two", HistoryStr(1, 0));
        Assert.Equal("one", HistoryStr(1, 1));
        Assert.Null(HistoryStr(1, 2));
        Assert.Equal("other", HistoryStr(2, 0));
    }

    [Fact]
    public void Should_DropOldest_And_HideFirstRecord_When_BlockFull()
    {
        // The block holds the empty first record (3 bytes) and two 4-letter strings (7 bytes each).
        HistorySize = 20;
        HistoryAdd(1, "aaaa");
        HistoryAdd(1, "bbbb");
        HistoryAdd(1, "cccc");

        // UPSTREAM: dropping the empty record and "aaaa" leaves "bbbb" first in the block, where it is skipped.
        Assert.Equal(1, HistoryCount(1));
        Assert.Equal("cccc", HistoryStr(1, 0));
    }

    [Fact]
    public void Should_DrawArrow()
    {
        var (screen, _, _) = Setup();

        Assert.Equal("▐↓▌", screen.Row(2)[20..23]);
    }

    [Fact]
    public void Should_FillLineWithPick_When_HistoryOpenedAndEnterPressed()
    {
        var (screen, line, history) = Setup();
        HistoryAdd(1, "first");
        HistoryAdd(1, "second");
        HistoryAdd(1, "third");
        line.Data = "typed";

        // The viewer starts on the second string; move down once more.
        screen.Events.Enqueue(Key(kbDown));
        screen.Events.Enqueue(Key(kbEnter));
        var ev = Views.Ev.MouseDown(21, 2);
        history.HandleEvent(ref ev);

        Assert.Equal(evNothing, ev.What);
        // Opening recorded the line's text as the newest string. The pick is cut to the line's limit.
        Assert.Equal("typed", HistoryStr(1, 3));
        Assert.Equal("thi", line.Data);
        Assert.Equal((0, 3), (line.SelStart, line.SelEnd));
    }

    [Fact]
    public void Should_KeepLine_When_HistoryCancelled()
    {
        var (screen, line, history) = Setup();
        HistoryAdd(1, "first");
        HistoryAdd(1, "second");
        line.Data = "abc";

        screen.Events.Enqueue(Key(kbEsc));
        var ev = Key(kbDown);
        history.HandleEvent(ref ev);

        Assert.Equal("abc", line.Data);
    }

    [Fact]
    public void Should_RecordText_When_LineLosesFocusOrRecordHistoryBroadcast()
    {
        var (_, line, history) = Setup();
        line.Data = "abc";

        var ev = new TEvent { What = evBroadcast, Message = new MessageEvent { Command = cmReleasedFocus, InfoPtr = line } };
        history.HandleEvent(ref ev);
        line.Data = "xyz";
        ev = new TEvent { What = evBroadcast, Message = new MessageEvent { Command = cmRecordHistory } };
        history.HandleEvent(ref ev);

        Assert.Equal(2, HistoryCount(1));
        Assert.Equal("xyz", HistoryStr(1, 1));
    }
}
