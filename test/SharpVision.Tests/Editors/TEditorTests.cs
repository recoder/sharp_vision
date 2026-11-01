using System;
using System.Globalization;
using System.Linq;

using SharpVision.Editors;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Tests.Dialogs;
using SharpVision.Tests.Views;
using Xunit;

using static SharpVision.Editors.EditorCommands;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;

namespace SharpVision.Tests.Editors;

// Ported from test/tvision/teditor.test.cpp.
public sealed class TEditorTests : CommandStateTest
{
    private const string Smile = "\U0001F60A";
    private const string Zigzag = "͛";

    private static TEditor NewEditor() => new(new TRect(0, 0, 0, 0), null, null, null, 256);

    // The text before the gap.
    private static string Text(TEditor e) => new(e.Buffer, 0, e.BufLen);

    // The whole text, across the gap.
    private static string AllText(TEditor e)
    {
        var buf = new char[e.BufLen];
        e.GetText(0, buf);
        return new string(buf);
    }

    [Theory]
    [InlineData("a\nb\n", "c\n", "a\nb\nc\n", TEditor.LineEndingType.eolLf)]
    [InlineData("a\r\nb\r\n", "c\n", "a\r\nb\r\nc\r\n", TEditor.LineEndingType.eolCrLf)]
    [InlineData("a\nb\n", "c\r\n", "a\nb\nc\n", TEditor.LineEndingType.eolLf)]
    [InlineData("a\rb\r", "c\n", "a\rb\rc\r", TEditor.LineEndingType.eolCr)]
    [InlineData("a", "\nb", "a\r\nb", TEditor.LineEndingType.eolCrLf)]
    public void Should_DetectAndConvertLineEndings_When_InsertingText(string initial, string insert, string expected, TEditor.LineEndingType type)
    {
        var e = NewEditor();
        // The initial text goes at the end of the buffer, after the gap.
        initial.CopyTo(e.Buffer.AsSpan(e.BufSize - initial.Length));
        e.SetBufLen(initial.Length);
        e.SetCurPtr(e.BufLen, 0);

        e.InsertText(insert, false);

        // The cursor is at the end, so the whole text is before the gap.
        Assert.Equal((expected, type), (Text(e), e.LineEnding));
    }

    [Theory]
    [InlineData(TEditor.LineEndingType.eolCrLf, "\r\n")]
    [InlineData(TEditor.LineEndingType.eolLf, "\n")]
    [InlineData(TEditor.LineEndingType.eolCr, "\r")]
    public void Should_InsertNewLines_With_LineEndingType(TEditor.LineEndingType type, string expected)
    {
        var e = NewEditor();
        e.LineEnding = type;

        e.NewLine();

        Assert.Equal(expected, Text(e));
    }

    private static TEvent Cmd(ushort command) => Ev.Command(command);

    private static TEvent Key(ushort keyCode, ushort state = 0) =>
        new() { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = keyCode, ControlKeyState = state } };

    private static TEvent Mouse(ushort what, int x, int y, ushort flags = 0) =>
        new() { What = what, Mouse = new MouseEventType { Where = new TPoint(x, y), Buttons = mbLeftButton, EventFlags = flags } };

    // Draws the text in a w x h editor after the events and a final scroll; returns the rows, each cell's
    // palette index (6 normal, 7 selected), and the cursor and scroll positions.
    private static (string[] Rows, string[] Attrs, TPoint CurPos, TPoint Delta) Display(
        int w, int h, string text, TEvent[] events, int dx = 0, int dy = 0)
    {
        var (screen, desk) = DialogDesk.Create(w, h);
        var e = new TEditor(new TRect(0, 0, w, h), null, null, null, 256);
        desk.Insert(e);
        foreach (var ev in events)
            screen.Events.Enqueue(ev);

        e.InsertText(text, false);
        e.SetCurPtr(0, 0);
        var input = default(TEvent);
        screen.GetEvent(ref input);
        while (input.What != evNothing)
        {
            e.HandleEvent(ref input);
            screen.GetEvent(ref input);
        }
        e.ScrollTo(dx, dy);

        var rows = Enumerable.Range(0, h).Select(screen.Row).ToArray();
        var attrs = Enumerable.Range(0, h)
            .Select(y => string.Concat(Enumerable.Range(0, w).Select(x => ((byte)screen.AttrAt(x, y)).ToString("X", CultureInfo.InvariantCulture))))
            .ToArray();
        return (rows, attrs, e.CurPos, e.Delta);
    }

    private static void AssertDisplay(
        (string[] Rows, string[] Attrs, TPoint CurPos, TPoint Delta) actual,
        string[] rows, string[] attrs, (int X, int Y) curPos, (int X, int Y) delta = default)
    {
        Assert.Equal(rows, actual.Rows);
        Assert.Equal(attrs, actual.Attrs);
        Assert.Equal(new TPoint(curPos.X, curPos.Y), actual.CurPos);
        Assert.Equal(new TPoint(delta.X, delta.Y), actual.Delta);
    }

    [Fact]
    public void Should_RestoreHalfDeletedCharacter_On_Undo()
    {
        // Upstream deletes one byte of "€"; here, one code unit of a surrogate pair.
        AssertDisplay(
            Display(3, 1, Smile,
                [Cmd(cmTextEnd), Cmd(cmEncoding), Key(kbLeft), Key(kbBack), Key(kbBack), Cmd(cmEncoding), Cmd(cmUndo)]),
            [Smile + " "], ["776"], (2, 0));
    }

    [Fact]
    public void Should_DrawCodeUnits_In_SingleByteMode()
    {
        AssertDisplay(
            Display(4, 1, Smile, [Cmd(cmEncoding), Cmd(cmTextEnd)]),
            ["��  "], ["6666"], (2, 0));
    }

    [Fact]
    public void Should_DrawTab_When_ScrolledHorizontally()
    {
        AssertDisplay(Display(3, 1, "\ta", [Cmd(cmTextEnd)], 6), ["  a"], ["666"], (9, 0), (6, 0));
    }

    [Fact]
    public void Should_DrawPartiallyVisibleSelectedTab()
    {
        AssertDisplay(
            Display(3, 1, "\ta", [Cmd(cmTextEnd), Key(kbLeft), Key(kbLeft, kbShift)], 6),
            ["  a"], ["776"], (0, 0), (6, 0));
    }

    [Fact]
    public void Should_DrawSelectedLineBreak()
    {
        AssertDisplay(
            Display(3, 2, "ab\ncd", [Key(kbRight), Key(kbRight, kbShift), Key(kbRight, kbShift), Key(kbRight, kbShift)]),
            ["ab ", "cd "], ["677", "766"], (1, 1));
    }

    [Fact]
    public void Should_DrawDoubleWidthCharacter()
    {
        AssertDisplay(Display(3, 1, Smile + "a", []), [Smile + "a"], ["666"], (0, 0));
    }

    [Fact]
    public void Should_DrawPartiallyVisibleSelectedDoubleWidthCharacter()
    {
        AssertDisplay(
            Display(2, 1, Smile + "a", [Cmd(cmTextEnd), Key(kbLeft), Key(kbLeft, kbShift)], 1),
            [" a"], ["76"], (0, 0), (1, 0));
    }

    [Fact]
    public void Should_DrawCombiningCharacters()
    {
        AssertDisplay(
            Display(2, 1, $"a{Zigzag}b{Zigzag}c{Zigzag}d{Zigzag}", [], 1),
            [$"b{Zigzag}c{Zigzag}"], ["66"], (0, 0), (1, 0));
    }

    [Theory]
    // Ctrl+Right skips trailing whitespace instead of stopping on it.
    [InlineData(7, 1, "foo bar", new[] { cmWordRight }, 4, 0)]
    // Ctrl+Left does the same in reverse.
    [InlineData(7, 1, "foo bar", new[] { cmTextEnd, cmWordLeft, cmWordLeft }, 0, 0)]
    // Ctrl+Right reaches the punctuation before the next word...
    [InlineData(10, 1, "foo(); bar", new[] { cmWordRight }, 3, 0)]
    // ...then skips the punctuation and the whitespace after it together.
    [InlineData(10, 1, "foo(); bar", new[] { cmWordRight, cmWordRight }, 7, 0)]
    // Ctrl+Left reaches the same two points in reverse.
    [InlineData(10, 1, "foo(); bar", new[] { cmTextEnd, cmWordLeft, cmWordLeft }, 3, 0)]
    [InlineData(10, 1, "foo(); bar", new[] { cmTextEnd, cmWordLeft, cmWordLeft, cmWordLeft }, 0, 0)]
    // Ctrl+Right skips consecutive blank lines in one jump, and Ctrl+Left in reverse.
    [InlineData(3, 4, "foo\n\n\nbar", new[] { cmWordRight }, 0, 3)]
    [InlineData(3, 4, "foo\n\n\nbar", new[] { cmTextEnd, cmWordLeft, cmWordLeft }, 0, 0)]
    public void Should_MoveByWords(int w, int h, string text, ushort[] commands, int x, int y)
    {
        var actual = Display(w, h, text, commands.Select(Cmd).ToArray());

        Assert.Equal(text.Split('\n').Select(l => l.PadRight(w)), actual.Rows);
        Assert.All(actual.Attrs, a => Assert.Equal(new string('6', w), a));
        Assert.Equal(new TPoint(x, y), actual.CurPos);
    }

    [Fact]
    public void Should_DeletePunctuationAndFollowingBlanks_On_DelWord()
    {
        AssertDisplay(Display(10, 1, "foo(); bar", [Cmd(cmWordRight), Cmd(cmDelWord)]), ["foobar    "], ["6666666666"], (3, 0));
    }

    [Fact]
    public void Should_DeleteWordLeft()
    {
        AssertDisplay(Display(7, 1, "foo bar", [Cmd(cmTextEnd), Cmd(cmDelWordLeft)]), ["foo    "], ["6666666"], (4, 0));
    }

    [Theory]
    // A word, a run of whitespace, a run of punctuation.
    [InlineData(9, 1, "foo   bar", 1, "777666666", 3)]
    [InlineData(9, 1, "foo   bar", 4, "666777666", 6)]
    [InlineData(9, 1, "foo();bar", 4, "666777666", 6)]
    public void Should_SelectRun_On_DoubleClick(int w, int h, string text, int x, string attrs, int curX)
    {
        AssertDisplay(
            Display(w, h, text, [Mouse(evMouseDown, x, 0, meDoubleClick), Mouse(evMouseUp, x, 0)]),
            [text], [attrs], (curX, 0));
    }

    [Fact]
    public void Should_NotSelectAcrossLineBreak_On_DoubleClickOnTrailingBlank()
    {
        AssertDisplay(
            Display(4, 2, "foo \n bar", [Mouse(evMouseDown, 3, 0, meDoubleClick), Mouse(evMouseUp, 3, 0)]),
            ["foo ", " bar"], ["6667", "6666"], (4, 0));
    }

    [Fact]
    public void Should_UndoTyping_And_RestoreDeletedSelection()
    {
        var e = NewEditor();
        e.InsertText("hello world", false);
        e.SetSelect(0, 5, false);

        e.InsertText("bye", false);
        Assert.Equal("bye world", AllText(e));

        e.Undo();
        Assert.Equal("hello world", AllText(e));
        Assert.Equal((0, 5), (e.SelStart, e.SelEnd));
    }

    [Fact]
    public void Should_FindAndReplaceAll()
    {
        var saved = (TEditor.FindStr, TEditor.ReplaceStr, TEditor.EditorFlags);
        try
        {
            var e = NewEditor();
            e.InsertText("a cat, a Cat, a catalog", false);
            e.SetCurPtr(0, 0);
            TEditor.FindStr = "cat";
            TEditor.ReplaceStr = "dog";
            TEditor.EditorFlags = efWholeWordsOnly | efReplaceAll | efDoReplace;

            e.DoSearchReplace();

            Assert.Equal("a dog, a dog, a catalog", AllText(e));
        }
        finally
        {
            (TEditor.FindStr, TEditor.ReplaceStr, TEditor.EditorFlags) = saved;
        }
    }

    [Fact]
    public void Should_MapWordStarPrefixKeys()
    {
        var e = NewEditor();
        e.InsertText("ab\ncd", false);

        // Ctrl-Q R: text start; Ctrl-Q D: line end.
        foreach (var k in new[] { kbCtrlQ, (ushort)'r', kbCtrlQ, (ushort)'d' })
        {
            var ev = Key(k);
            e.HandleEvent(ref ev);
        }

        Assert.Equal(2, e.CurPtr);
    }

    [Fact]
    public void Should_ShowPosition_And_ModifiedMark_In_Indicator()
    {
        var (screen, desk) = DialogDesk.Create(12, 2);
        var ind = new TIndicator(new TRect(0, 1, 12, 2));
        var e = new TEditor(new TRect(0, 0, 12, 1), null, null, ind, 64);
        desk.Insert(ind);
        desk.Insert(e);

        e.InsertText("ab", false);

        // The colon goes to column 8.
        Assert.Equal("☼═════ 1:3 ═", screen.Row(1));
    }
}
