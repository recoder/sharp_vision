using SharpVision.Editors;
using SharpVision.Objects;
using SharpVision.Tests.Dialogs;
using SharpVision.Tests.Views;
using Xunit;

namespace SharpVision.Tests.Editors;

public sealed class TTerminalTests : CommandStateTest
{
    private sealed class TestTerminal : TTerminal
    {
        public TestTerminal(int queBack, int queFront, string buffer)
            : base(new TRect(0, 0, 0, 0), null, null, 8)
        {
            buffer.PadRight(8, '\0').CopyTo(Buffer);
            QueBack = queBack;
            QueFront = queFront;
        }
    }

    // Ported from test/tvision/textview.test.cpp.
    [Theory]
    [InlineData(0, 0, "", 0, 1, 0)]
    [InlineData(0, 7, "ab\ncd\ne", 7, 1, 6)]
    [InlineData(0, 7, "ab\ncd\ne", 6, 1, 6)]
    [InlineData(0, 7, "ab\ncd\ne", 5, 1, 3)]
    [InlineData(0, 7, "ab\ncd\ne", 4, 1, 3)]
    [InlineData(0, 7, "ab\ncd\ne", 3, 1, 3)]
    [InlineData(0, 7, "ab\ncd\ne", 2, 1, 0)]
    [InlineData(0, 7, "ab\ncd\ne", 1, 1, 0)]
    [InlineData(0, 7, "ab\ncd\ne", 0, 1, 0)]
    [InlineData(0, 4, "a\nb\n", 4, 1, 4)]
    [InlineData(0, 4, "a\nb\n", 4, 2, 2)]
    [InlineData(0, 4, "a\nb\n", 4, 3, 0)]
    [InlineData(0, 4, "a\nb\n", 3, 1, 2)]
    [InlineData(0, 4, "a\nb\n", 3, 2, 0)]
    [InlineData(0, 4, "a\nb\n", 3, 3, 0)]
    [InlineData(4, 3, "aaa\0aaaa", 3, 1, 4)]
    [InlineData(4, 3, "a\na\0aaaa", 5, 1, 4)]
    [InlineData(7, 0, "\0\0\0\0\0\0\0a", 0, 1, 7)]
    public void Should_FindPreviousLines(int queBack, int queFront, string buffer, int pos, int lines, int expected)
    {
        Assert.Equal(expected, new TestTerminal(queBack, queFront, buffer).PrevLines(pos, lines));
    }

    private static (TestScreen Screen, TTerminal Terminal) Open(int bufSize)
    {
        var (screen, desk) = DialogDesk.Create(6, 3);
        var t = new TTerminal(new TRect(0, 0, 6, 3), null, null, bufSize);
        desk.Insert(t);
        return (screen, t);
    }

    [Fact]
    public void Should_ShowLines_With_CursorAfterTheText()
    {
        var (screen, t) = Open(100);

        t.DoSputn("ab\ncd\ne");

        Assert.Equal(["ab    ", "cd    ", "e     "], new[] { screen.Row(0), screen.Row(1), screen.Row(2) });
        Assert.Equal((1, 2), (t.Cursor.X, t.Cursor.Y));
    }

    [Fact]
    public void Should_DropOldestLines_When_BufferIsFull()
    {
        var (screen, t) = Open(8);

        t.DoSputn("aa\n");
        t.DoSputn("bb\n");
        t.DoSputn("cc\n");

        Assert.Equal(["bb    ", "cc    ", "      "], new[] { screen.Row(0), screen.Row(1), screen.Row(2) });
        Assert.False(t.QueEmpty());
    }
}
