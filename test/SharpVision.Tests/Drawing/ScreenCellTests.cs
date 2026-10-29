// Ported from test/platform/scrncell.test.cpp.

using SharpVision.Drawing;
using Xunit;

namespace SharpVision.Tests.Drawing;

public sealed class TScreenCharacterTests
{
    [Fact]
    public void Should_HoldCharacter_When_ConstructedFromChar()
    {
        TScreenCharacter ch = 'A';

        Assert.Equal("A", ch.GetText());
        Assert.True(ch.IsSingleChar);
        Assert.Equal('A', ch.SingleChar);
        Assert.False(ch.IsWide);
        Assert.False(ch.IsWideCharTrail);
    }

    [Fact]
    public void Should_ResetState_When_InitializedWithChar()
    {
        var ch = default(TScreenCharacter);
        ch.InitAsWideCharTrail();
        ch.InitWithChar('B');

        Assert.Equal("B", ch.GetText());
        Assert.False(ch.IsWideCharTrail);
    }

    [Theory]
    [InlineData("é", false)]
    [InlineData("中", true)]
    [InlineData("😀", true)]
    public void Should_HoldText_When_InitializedWithText(string text, bool wide)
    {
        var ch = default(TScreenCharacter);
        ch.InitAsWideCharTrail();
        ch.InitWithText(text, wide);

        Assert.Equal(text, ch.GetText());
        Assert.Equal(wide, ch.IsWide);
        Assert.False(ch.IsWideCharTrail);
    }

    [Fact]
    public void Should_BeEmpty_When_InitializedWithMoreThanOneCharacter()
    {
        var ch = default(TScreenCharacter);
        ch.InitWithText("abc");

        Assert.Equal("\0", ch.GetText());
    }

    [Fact]
    public void Should_ClearWideFlag_When_InitializedAsTrail()
    {
        var ch = default(TScreenCharacter);
        ch.InitWithText("中", true);
        ch.InitAsWideCharTrail();

        Assert.True(ch.IsWideCharTrail);
        Assert.False(ch.IsWide);
    }

    [Fact]
    public void Should_AppendCombiningCharacter()
    {
        TScreenCharacter ch = 'a';
        ch.AppendZeroWidthChar("̀");

        Assert.Equal("à", ch.GetText());
        Assert.False(ch.IsSingleChar);
    }

    [Fact]
    public void Should_PromoteNullToSpace_When_AppendingToEmptyCell()
    {
        var ch = default(TScreenCharacter);
        ch.AppendZeroWidthChar("̀");

        Assert.Equal(" ̀", ch.GetText());
    }

    [Fact]
    public void Should_IgnoreLaterAppends_When_TextOverflows()
    {
        var ch = default(TScreenCharacter);
        ch.InitWithText("😀");                // 2 code units.
        for (var n = 0; n < 6; ++n)
            ch.AppendZeroWidthChar("\U000E0100"); // +2 each = 14.
        ch.AppendZeroWidthChar("\U000E0100");     // 16 > 15: overflow.
        ch.AppendZeroWidthChar("̀");         // Would fit, but overflow is sticky.

        Assert.Equal(14, ch.GetText().Length);
    }
}

public sealed class TScreenCellTests
{
    [Fact]
    public void Should_HoldCharacterAndAttribute()
    {
        var cell = new TScreenCell('X', new TColorAttr(TColor.FromBIOS(0x7), TColor.FromBIOS(0xF)));

        Assert.Equal("X", cell.Character.GetText());
        Assert.Equal(new TColorAttr(0xF7), cell.Attribute);
    }

    [Fact]
    public void Should_TranslateCodePage_When_ConstructedFromBIOS()
    {
        Assert.Equal(new TScreenCell('A', 0x1F), new TScreenCell(0x1F41));
        Assert.Equal(new TScreenCell('│', 0x1F), new TScreenCell(0x1FB3));
    }

    [Fact]
    public void Should_CompareCharacterAndAttribute()
    {
        var a = new TScreenCell('A', 0x07);

        Assert.Equal(new TScreenCell('A', 0x07), a);
        Assert.NotEqual(new TScreenCell('B', 0x07), a);
        Assert.NotEqual(new TScreenCell('A', 0x70), a);
        Assert.Equal(default, new TScreenCell('\0', default));
    }
}
