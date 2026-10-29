using System;
using System.Linq;
using SharpVision.Drawing;
using Xunit;

namespace SharpVision.Tests.Drawing;

public sealed class TDrawBufferTests
{
    private static string Text(TDrawBuffer b, int start, int count) =>
        string.Concat(b.Data.Skip(start).Take(count).Select(c => c.Character.GetText()));

    [Fact]
    public void Should_ToggleAttributes_When_MovingCStr()
    {
        var b = new TDrawBuffer();
        var attrs = new TAttrPair(0x70, 0x74);

        var width = b.MoveCStr(0, "~F~ile", attrs);

        Assert.Equal(4, width);
        Assert.Equal("File", Text(b, 0, 4));
        Assert.Equal(new TColorAttr(0x74), b.Data[0].Attribute);
        Assert.All(b.Data[1..4], c => Assert.Equal(new TColorAttr(0x70), c.Attribute));
    }

    [Fact]
    public void Should_SkipColumnsButKeepToggling_When_MovingCStrWithIndent()
    {
        var b = new TDrawBuffer();

        var width = b.MoveCStr(0, "~F~ile", new TAttrPair(0x70, 0x74), strIndent: 1);

        Assert.Equal(3, width);
        Assert.Equal("ile", Text(b, 0, 3));
        Assert.Equal(new TColorAttr(0x70), b.Data[0].Attribute);
    }

    [Fact]
    public void Should_StopAtMaxWidth_When_MovingCStr()
    {
        var b = new TDrawBuffer();

        Assert.Equal(2, b.MoveCStr(3, "abcdef", 0x07, maxStrWidth: 2));
        Assert.Equal("\0", Text(b, 5, 1));
    }

    [Fact]
    public void Should_KeepAttributes_When_MovingStrWithZeroAttr()
    {
        var b = new TDrawBuffer();
        b.MoveChar(0, ' ', 0x1F, 10);

        var width = b.MoveStr(2, "hello", 0);

        Assert.Equal(5, width);
        Assert.Equal("  hello   ", Text(b, 0, 10));
        Assert.All(b.Data[..10], c => Assert.Equal(new TColorAttr(0x1F), c.Attribute));
    }

    [Fact]
    public void Should_FillOnlyAttributes_When_MovingNullChar()
    {
        var b = new TDrawBuffer();
        b.MoveStr(0, "abc", 0x07);

        b.MoveChar(0, '\0', 0x70, 3);

        Assert.Equal("abc", Text(b, 0, 3));
        Assert.All(b.Data[..3], c => Assert.Equal(new TColorAttr(0x70), c.Attribute));
    }

    [Fact]
    public void Should_FillOnlyCharacters_When_MovingZeroAttr()
    {
        var b = new TDrawBuffer();
        b.MoveStr(0, "abc", 0x07);

        b.MoveChar(0, 'x', 0, 3);

        Assert.Equal("xxx", Text(b, 0, 3));
        Assert.All(b.Data[..3], c => Assert.Equal(new TColorAttr(0x07), c.Attribute));
    }

    [Fact]
    public void Should_ClipToCapacity()
    {
        var b = new TDrawBuffer();
        var end = b.Capacity;

        b.MoveChar(end - 2, 'x', 0x07, 10);
        b.PutChar(end, 'y');
        b.PutAttribute(end, 0x70);

        Assert.Equal("xx", Text(b, end - 2, 2));
        Assert.Equal(0, b.MoveStr(end, "abc", 0x07));
        Assert.Equal(2, b.MoveStr(end - 2, "abc", 0x07));
    }

    [Fact]
    public void Should_DrawNothing_When_IndentIsNegative()
    {
        var b = new TDrawBuffer();

        b.MoveChar(-1, 'x', 0x07, 5);

        Assert.Equal(0, b.MoveStr(-1, "abc", 0x07));
        Assert.All(b.Data, c => Assert.Equal(default, c));
    }

    [Fact]
    public void Should_CopyOnlyCount_When_MovingBuf()
    {
        var b = new TDrawBuffer();

        b.MoveBuf(0, "abcdef", 0x07, 3);
        b.MoveBuf(5, "xy", 0x07, 10);

        Assert.Equal("abc\0\0xy", Text(b, 0, 7));
    }

    [Fact]
    public void Should_FitDefaultScreen()
    {
        Assert.True(new TDrawBuffer().Capacity >= 88);
    }

    [Fact]
    public void Should_SetSingleCell_When_PuttingCharAndAttribute()
    {
        var b = new TDrawBuffer();

        b.PutChar(1, 'z');
        b.PutAttribute(1, 0x4E);

        Assert.Equal(new TScreenCell('z', 0x4E), b.Data[1]);
        Assert.Equal(default, b.Data.AsSpan()[0]);
    }
}
