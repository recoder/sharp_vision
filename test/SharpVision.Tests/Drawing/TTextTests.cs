// Ported from test/platform/ttext.test.cpp. Upstream feeds UTF-8 bytes and mocks the
// character width function; these tests feed UTF-16 strings and use the real one.

using System;
using System.Linq;
using SharpVision.Drawing;
using Xunit;

namespace SharpVision.Tests.Drawing;

public sealed class TTextTests
{
    private const string CombiningZigzag = "͛";
    private const string DevanagariVirama = "्";
    private const string ZeroWidthJoiner = "‍";

    [Theory]
    [InlineData("", 0)]
    [InlineData("\0", 0)]
    [InlineData("a", (byte)'a')]
    [InlineData("○", (byte)'\t')]
    [InlineData("\t", (byte)'\t')]
    [InlineData("≡", 0xF0)]
    [InlineData("€", 0)]
    public void Should_ConvertToCodePage(string input, byte expected)
    {
        Assert.Equal(expected, TText.ToCodePage(input));
    }

    [Theory]
    [InlineData(0, "\0")]
    [InlineData((byte)'a', "a")]
    [InlineData((byte)'\t', "○")]
    [InlineData(0xF0, "≡")]
    public void Should_ConvertFromCodePage(byte input, string expected)
    {
        Assert.Equal(expected, TText.FromCodePage(input));
    }

    [Theory]
    [InlineData("\0", "\0")]
    [InlineData("\x01", "☺")]
    [InlineData("\x1F", "▼")]
    [InlineData(" ", " ")]
    [InlineData("~", "~")]
    [InlineData("\x7F", "⌂")]
    [InlineData("\u0080", "�")]
    [InlineData("þ", "þ")]
    [InlineData("\uD800", "�")]
    [InlineData("\x03" + CombiningZigzag, "♥" + CombiningZigzag)]
    [InlineData("ñ" + CombiningZigzag, "ñ" + CombiningZigzag)]
    public void Should_ConvertControlCharacters_When_Drawing(string input, string expected)
    {
        var cells = new TScreenCell[1];
        TText.DrawStr(cells, input);

        Assert.Equal(expected, cells[0].Character.GetText());
    }

    [Theory]
    [InlineData("a", "a", "\0")]
    [InlineData("aa", "a", "a")]
    [InlineData("\0", "\0", "\0")]
    [InlineData("\0\0", "\0", "\0")]
    [InlineData("\0a", "\0", "a")]
    [InlineData("a" + CombiningZigzag, "a" + CombiningZigzag, "\0")]
    [InlineData("\0" + CombiningZigzag, " " + CombiningZigzag, "\0")]
    [InlineData("क" + DevanagariVirama + ZeroWidthJoiner + "ष", "क" + DevanagariVirama, "ष")]
    public void Should_DrawTextInScreenCells(string input, string cell0, string cell1)
    {
        var cells = new TScreenCell[2];
        TText.DrawStr(cells, input);

        Assert.Equal([cell0, cell1], cells.Select(c => c.Character.GetText()));
    }

    [Fact]
    public void Should_DrawTrail_When_DrawingWideCharacter()
    {
        var cells = new TScreenCell[3];
        var count = TText.DrawStr(cells, "中a", 0x1F);

        Assert.Equal(3, count);
        Assert.True(cells[0].Character.IsWide);
        Assert.True(cells[1].Character.IsWideCharTrail);
        Assert.Equal("a", cells[2].Character.GetText());
        Assert.All(cells, c => Assert.Equal(new TColorAttr(0x1F), c.Attribute));
    }

    [Fact]
    public void Should_OmitTrail_When_WideCharacterDoesNotFit()
    {
        var cells = new TScreenCell[2];
        var count = TText.DrawStr(cells, "a中");

        Assert.Equal(2, count);
        Assert.True(cells[1].Character.IsWide);
        Assert.False(cells[1].Character.IsWideCharTrail);
    }

    [Fact]
    public void Should_DrawSpace_When_TextIndentSplitsWideCharacter()
    {
        var cells = new TScreenCell[3];
        TText.DrawStr(cells, 0, "中ab", 1);

        Assert.Equal([" ", "a", "b"], cells.Select(c => c.Character.GetText()));
    }

    [Fact]
    public void Should_TransformAttributes_When_DrawingWithCallback()
    {
        var cells = new TScreenCell[2];
        cells.AsSpan().Fill(new TScreenCell(' ', 0x10));
        TText.DrawStrEx(cells, 0, "ab", 0, (ref TColorAttr attr) => attr = attr.Reversed());

        Assert.All(cells, c => Assert.Equal(new TColorAttr(0x01), c.Attribute));
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("abc", 3)]
    [InlineData("中文", 4)]
    [InlineData("a" + CombiningZigzag, 1)]
    [InlineData("😊", 2)]
    public void Should_MeasureColumns(string text, int expected)
    {
        Assert.Equal(expected, TText.Width(text));
    }

    [Fact]
    public void Should_CountGraphemes_When_Measuring()
    {
        Assert.Equal(new TTextMetrics(3, 3, 2), TText.Measure("a" + CombiningZigzag + "中"));
    }

    [Theory]
    [InlineData("a中b", 2, false, 1, 1)]
    [InlineData("a中b", 2, true, 2, 3)]
    [InlineData("a中b", 3, false, 2, 3)]
    [InlineData("abc", -1, false, 0, 0)]
    [InlineData("ab", 5, false, 2, 2)]
    [InlineData("😊x", 2, false, 2, 2)]
    public void Should_FindLengthOfColumns_When_Scrolling(string text, int count, bool includeIncomplete, int length, int width)
    {
        TText.Scroll(text, count, includeIncomplete, out var actualLength, out var actualWidth);

        Assert.Equal((length, width), (actualLength, actualWidth));
    }

    [Fact]
    public void Should_StepOverSurrogatePairs()
    {
        Assert.Equal(2, TText.Next("😊b"));
        Assert.Equal(1, TText.Next("\uD800b"));
        Assert.Equal(0, TText.Next(""));
        Assert.Equal(2, TText.Prev("a😊", 3));
        Assert.Equal(1, TText.Prev("ab", 1));
        Assert.Equal(0, TText.Prev("ab", 0));
    }

    [Theory]
    [InlineData("FileФЫ", "fileфы", true)]
    [InlineData("a", "ab", false)]
    [InlineData("", "", true)]
    public void Should_IgnoreCase_When_Comparing(string a, string b, bool expected)
    {
        Assert.Equal(expected, TText.EqualsIgnoreCase(a, b));
    }
}

[Collection(GlobalStateCollection.Name)]
public sealed class TTextCodePageTests
{
    [Fact]
    public void Should_OverrideCodePageTranslation()
    {
        var custom = new string?[256];
        custom[0x10] = "¥";           // ASCII position.
        custom[0x80] = "\U00010000";  // Non-ASCII position, surrogate pair.
        try
        {
            TText.SetCodePageTranslation(custom);

            Assert.Equal(0x10, TText.ToCodePage("¥"));
            Assert.Equal(0x10, TText.ToCodePage("\x10"));
            Assert.Equal(0x20, TText.ToCodePage("\x20"));
            Assert.Equal(0x80, TText.ToCodePage("\U00010000"));
            Assert.Equal("¥", TText.FromCodePage(0x10));
            Assert.Equal("\0", TText.FromCodePage(0x20));
            Assert.Equal("\U00010000", TText.FromCodePage(0x80));
        }
        finally
        {
            TText.SetCodePageTranslation(null);
        }

        Assert.Equal("►", TText.FromCodePage(0x10));
        Assert.Equal(" ", TText.FromCodePage(0x20));
        Assert.Equal("Ç", TText.FromCodePage(0x80));
    }
}
