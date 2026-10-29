using System;
using System.Collections.Generic;
using SharpVision.Drawing;
using SharpVision.Objects;
using SharpVision.Platform;
using Xunit;

namespace SharpVision.Tests.Platform;

public sealed class DisplayBufferTests
{
    private sealed class RecordingDisplay(int width, int height) : DisplayAdapter
    {
        public List<string> Writes { get; } = new();

        public int Flushes { get; private set; }

        public override TPoint ReloadScreenInfo() => new(width, height);

        public override void WriteCell(TPoint pos, ReadOnlySpan<char> text, TColorAttr attr, bool doubleWidth) =>
            Writes.Add($"{pos.X},{pos.Y}:{text.ToString()}");

        public override void Flush() => Flushes++;
    }

    private static (DisplayBuffer, RecordingDisplay) Create(int w, int h)
    {
        var display = new RecordingDisplay(w, h);
        var buffer = new DisplayBuffer(0); // No frame rate limit.
        buffer.ReloadScreenInfo(display);
        return (buffer, display);
    }

    private static TScreenCell[] Cells(string text, TColorAttr attr)
    {
        var cells = new TScreenCell[TText.Width(text)];
        TText.DrawStr(cells, text, attr);
        return cells;
    }

    [Fact]
    public void Should_WriteOnlyChangedCells()
    {
        var (buffer, display) = Create(5, 2);
        buffer.ScreenWrite(0, 0, Cells("abcde", 0x07));
        buffer.FlushScreen(display);
        display.Writes.Clear();

        buffer.ScreenWrite(0, 0, Cells("abXde", 0x07));
        buffer.FlushScreen(display);

        Assert.Equal(["2,0:X"], display.Writes);
    }

    [Fact]
    public void Should_PrintEmptyCellsAsSpacesAndControlsAsGlyphs()
    {
        var (buffer, display) = Create(3, 1);
        var cells = new TScreenCell[3];
        cells[1].Character = '\x10';
        cells[2].Attribute = 0x1F;

        buffer.ScreenWrite(0, 0, cells);
        buffer.FlushScreen(display);

        Assert.Equal(["0,0: ", "1,0:►", "2,0: "], display.Writes);
    }

    [Fact]
    public void Should_ClipWritesToScreen()
    {
        var (buffer, display) = Create(3, 1);

        buffer.ScreenWrite(1, 0, Cells("abcd", 0x07));
        buffer.ScreenWrite(0, 5, Cells("zz", 0x07));
        buffer.FlushScreen(display);

        Assert.Equal(["1,0:a", "2,0:b"], display.Writes);
    }

    [Fact]
    public void Should_WriteWideCharacterOnce()
    {
        var (buffer, display) = Create(4, 1);

        buffer.ScreenWrite(0, 0, Cells("中ab", 0x07));
        buffer.FlushScreen(display);

        Assert.Equal(["0,0:中", "2,0:a", "3,0:b"], display.Writes);
    }

    [Fact]
    public void Should_ReplaceWideCharacterWithSpace_When_ItWouldWrap()
    {
        var (buffer, display) = Create(3, 1);
        var cells = Cells("ab", 0x07);
        buffer.ScreenWrite(0, 0, cells);
        var wide = new TScreenCell[1];
        wide[0].Character.InitWithText("中", true);

        buffer.ScreenWrite(2, 0, wide);
        buffer.FlushScreen(display);

        Assert.Equal("2,0: ", display.Writes[^1]);
    }
}
