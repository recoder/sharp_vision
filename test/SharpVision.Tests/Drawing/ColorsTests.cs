// Ported from test/platform/colors.test.cpp.

using SharpVision.Drawing;
using Xunit;

using static SharpVision.Drawing.StyleFlags;

namespace SharpVision.Tests.Drawing;

public sealed class TColorRGBTests
{
    [Fact]
    public void Should_BeBlack_When_DefaultConstructed()
    {
        var rgb = new TColorRGB();

        Assert.Equal(0, rgb.Red);
        Assert.Equal(0, rgb.Green);
        Assert.Equal(0, rgb.Blue);
        Assert.Equal(0u, (uint)rgb);
    }

    [Fact]
    public void Should_ExposeComponents_When_ConstructedFromComponents()
    {
        var rgb = new TColorRGB(127, 0, 187);

        Assert.Equal(127, rgb.Red);
        Assert.Equal(0, rgb.Green);
        Assert.Equal(187, rgb.Blue);
        Assert.Equal(0x7F00BBu, (uint)rgb);
    }

    [Fact]
    public void Should_TruncateHighBits_When_ConstructedFromInteger()
    {
        var rgb = new TColorRGB(0xAABBCCDD);

        Assert.Equal(0xBB, rgb.Red);
        Assert.Equal(0xCC, rgb.Green);
        Assert.Equal(0xDD, rgb.Blue);
        Assert.Equal(0xBBCCDDu, (uint)rgb);
    }

    [Fact]
    public void Should_UpdateValue_When_ComponentsAreSet()
    {
        var rgb = new TColorRGB { Red = 255, Green = 128, Blue = 64 };

        Assert.Equal(0xFF8040u, (uint)rgb);
    }
}

public sealed class TColorBIOSTests
{
    [Fact]
    public void Should_ExposeBits_When_ConstructedFromValue()
    {
        var bios = new TColorBIOS(0b1101);

        Assert.True(bios.Blue);
        Assert.False(bios.Green);
        Assert.True(bios.Red);
        Assert.True(bios.Intensity);
    }

    [Fact]
    public void Should_TruncateHighBits_When_ConstructedFromInteger()
    {
        Assert.Equal(0x0B, (byte)new TColorBIOS(0xAB));
    }

    [Fact]
    public void Should_UpdateValue_When_BitsAreSet()
    {
        var bios = new TColorBIOS { Red = true, Green = true, Blue = false, Intensity = true };

        Assert.Equal(0b1110, (byte)bios);
    }
}

public sealed class TColorConversionTests
{
    [Theory]
    [InlineData(0x0, 0)]
    [InlineData(0x1, 4)]
    [InlineData(0x4, 1)]
    [InlineData(0x8, 8)]
    [InlineData(0xF, 15)]
    public void Should_SwapRedAndBlue_When_ConvertingBIOStoXTerm16(byte bios, byte xterm)
    {
        Assert.Equal(xterm, (byte)TColorConversion.BIOStoXTerm16(bios));
        Assert.Equal(bios, (byte)TColorConversion.XTerm16toBIOS(xterm));
    }

    [Theory]
    [InlineData(16, 0x000000)]
    [InlineData(17, 0x00005F)]
    [InlineData(21, 0x0000FF)]
    [InlineData(46, 0x00FF00)]
    [InlineData(59, 0x5F5F5F)]
    [InlineData(110, 0x87AFD7)]
    [InlineData(196, 0xFF0000)]
    [InlineData(208, 0xFF8700)]
    [InlineData(231, 0xFFFFFF)]
    [InlineData(232, 0x080808)]
    [InlineData(240, 0x585858)]
    [InlineData(255, 0xEEEEEE)]
    public void Should_MatchXTermPalette_When_ConvertingBetweenXTerm256AndRGB(byte xterm, int rgb)
    {
        Assert.Equal((uint)rgb, (uint)TColorConversion.XTerm256toRGB(xterm));
        Assert.Equal(xterm, (byte)TColorConversion.RGBtoXTerm256(rgb));
    }

    [Theory]
    [InlineData(21, 12)]
    [InlineData(46, 10)]
    [InlineData(51, 14)]
    [InlineData(59, 8)]
    [InlineData(196, 9)]
    [InlineData(201, 13)]
    [InlineData(226, 11)]
    [InlineData(231, 15)]
    [InlineData(232, 0)]
    [InlineData(244, 8)]
    public void Should_Approximate_When_ConvertingXTerm256toXTerm16(byte input, byte expected)
    {
        Assert.Equal(expected, (byte)TColorConversion.XTerm256toXTerm16(input));
    }

    [Theory]
    [InlineData(0x800000, 1)]
    [InlineData(0x008000, 2)]
    [InlineData(0x808000, 3)]
    [InlineData(0x000080, 4)]
    [InlineData(0x800080, 5)]
    [InlineData(0x008080, 6)]
    [InlineData(0x804000, 3)]
    [InlineData(0xFF0000, 9)]
    [InlineData(0x00FF00, 10)]
    [InlineData(0xFFFF00, 11)]
    [InlineData(0x0000FF, 12)]
    [InlineData(0xFF00FF, 13)]
    [InlineData(0x00FFFF, 14)]
    [InlineData(0x000000, 0)]
    [InlineData(0x404040, 8)]
    [InlineData(0xC0C0C0, 7)]
    [InlineData(0xFFFFFF, 15)]
    public void Should_ApproximateByHue_When_ConvertingRGBtoXTerm16(int rgb, byte expected)
    {
        Assert.Equal(expected, (byte)TColorConversion.RGBtoXTerm16(rgb));
    }
}

public sealed class TColorTests
{
    [Fact]
    public void Should_BeDefaultColor_When_DefaultConstructed()
    {
        var def = default(TColor);

        Assert.True(def.IsDefault);
        Assert.False(def.IsBIOS);
        Assert.False(def.IsRGB);
        Assert.False(def.IsXTerm);
        Assert.Equal(TColor.Default, def);
    }

    [Fact]
    public void Should_KeepTypeAndValue_When_ConvertedFromEachEncoding()
    {
        TColor bios = new TColorBIOS(0xD);
        TColor rgb = new TColorRGB(0x12, 0x34, 0x56);
        TColor xterm = new TColorXTerm(0xAB);

        Assert.True(bios.IsBIOS);
        Assert.Equal(new TColorBIOS(0xD), bios.AsBIOS());
        Assert.True(rgb.IsRGB);
        Assert.Equal(new TColorRGB(0x12, 0x34, 0x56), rgb.AsRGB());
        Assert.True(xterm.IsXTerm);
        Assert.Equal(new TColorXTerm(0xAB), xterm.AsXTerm());
    }

    [Fact]
    public void Should_QuantizeToBIOS_When_ConvertingAnyEncoding()
    {
        Assert.Equal(0x7, (byte)TColor.FromBIOS(0x7).ToBIOS(true));
        Assert.Equal(0xE, (byte)TColor.FromRGB(0xEBDB34).ToBIOS(true));
        Assert.Equal(0xD, (byte)TColor.FromXTerm(201).ToBIOS(true));
    }

    [Fact]
    public void Should_UseGrayOnBlack_When_ConvertingDefaultToBIOS()
    {
        Assert.Equal(0x7, (byte)TColor.Default.ToBIOS(true));
        Assert.Equal(0x0, (byte)TColor.Default.ToBIOS(false));
    }

    [Fact]
    public void Should_NotBeEqual_When_TypesDiffer()
    {
        Assert.Equal(TColor.FromRGB(0x7F00BB), TColor.FromRGB(0x7F00BB));
        Assert.NotEqual(TColor.FromRGB(0x7F00BB), TColor.FromRGB(0x7F00BC));
        Assert.NotEqual(TColor.FromBIOS(0), TColor.FromRGB(0));
    }
}

public sealed class TColorAttrTests
{
    [Fact]
    public void Should_HaveDefaultColorsAndNoStyle_When_DefaultConstructed()
    {
        var attr = default(TColorAttr);

        Assert.True(attr.Foreground.IsDefault);
        Assert.True(attr.Background.IsDefault);
        Assert.Equal(0, attr.Style);
    }

    [Fact]
    public void Should_SplitNibbles_When_ConstructedFromBIOSInt()
    {
        TColorAttr attr = 0x3D;

        Assert.Equal(TColor.FromBIOS(0xD), attr.Foreground);
        Assert.Equal(TColor.FromBIOS(0x3), attr.Background);
        Assert.Equal(0, attr.Style);
        Assert.True(attr == 0x3D);
        Assert.True(attr != 0x3E);
    }

    [Fact]
    public void Should_StoreColorsAndStyle_When_ConstructedFromColors()
    {
        var attr = new TColorAttr(TColor.FromRGB(0xFF0000), TColor.FromBIOS(0x1), slBold | slItalic);

        Assert.Equal(TColor.FromRGB(0xFF0000), attr.Foreground);
        Assert.Equal(TColor.FromBIOS(0x1), attr.Background);
        Assert.Equal(slBold | slItalic, attr.Style);
    }

    [Fact]
    public void Should_UpdateFields_When_PropertiesAreSet()
    {
        var attr = new TColorAttr
        {
            Foreground = TColor.FromRGB(0x123456),
            Background = TColor.FromBIOS(0x7),
            Style = slUnderline,
        };

        Assert.Equal(TColor.FromRGB(0x123456), attr.Foreground);
        Assert.Equal(TColor.FromBIOS(0x7), attr.Background);
        Assert.Equal(slUnderline, attr.Style);
    }

    [Fact]
    public void Should_ToggleReverseStyle_When_ReversingDefaultColors()
    {
        var withStyle = new TColorAttr(TColor.FromBIOS(0x7), TColor.FromBIOS(0xF), slBold);

        Assert.Equal(new TColorAttr(default, default, slReverse), default(TColorAttr).Reversed());
        Assert.Equal(new TColorAttr(TColor.FromBIOS(0xF), TColor.FromBIOS(0x7), slBold), withStyle.Reversed());
    }

    [Fact]
    public void Should_Return5F_When_CastingNonBIOSAttrToByte()
    {
        Assert.Equal(0xF7, (byte)new TColorAttr(TColor.FromBIOS(0x7), TColor.FromBIOS(0xF)));
        Assert.Equal(0x5F, (byte)new TColorAttr(TColor.FromRGB(0xFF0000), TColor.FromBIOS(0xF)));
    }

    [Fact]
    public void Should_QuantizeAndIgnoreStyle_When_ConvertingToBIOS()
    {
        Assert.Equal(0xF7, new TColorAttr(TColor.FromBIOS(0x7), TColor.FromBIOS(0xF)).ToBIOS());
        Assert.Equal(0x1C, new TColorAttr(TColor.FromRGB(0xFF0000), TColor.FromRGB(0x000080)).ToBIOS());
        Assert.Equal(0xF7, new TColorAttr(TColor.FromBIOS(0x7), TColor.FromBIOS(0xF), slBold).ToBIOS());
    }

    [Fact]
    public void Should_TakeLowAttribute_When_ConstructedFromPair()
    {
        var low = new TColorAttr(TColor.FromRGB(0xFF0000), TColor.FromBIOS(0x1), slBold);

        Assert.Equal(low, new TColorAttr(new TAttrPair(low, 0x7)));
    }

    [Fact]
    public void Should_MakeHighAttribute_When_ShiftingLeftBy8()
    {
        TColorAttr attr = 0x7;
        var pair = attr << 8;

        Assert.Equal(new TColorAttr(0), pair[0]);
        Assert.Equal(attr, pair[1]);
        Assert.Equal(0x70, (ushort)(attr << 4));
    }
}

public sealed class TAttrPairTests
{
    [Fact]
    public void Should_SplitBytes_When_ConstructedFromInt()
    {
        TAttrPair pair = 0x3D21;

        Assert.Equal(new TColorAttr(0x21), pair[0]);
        Assert.Equal(new TColorAttr(0x3D), pair[1]);
        Assert.Equal(0x3D21, (ushort)pair);
    }

    [Fact]
    public void Should_DefaultHighToBIOSZero_When_OnlyLowGiven()
    {
        var pair = new TAttrPair(0x7);

        Assert.Equal(new TColorAttr(0x7), pair[0]);
        Assert.Equal(new TColorAttr(0x00), pair[1]);
    }

    [Fact]
    public void Should_MoveHighToLow_When_ShiftingRightBy8()
    {
        var pair = new TAttrPair(0x1, 0x2) >> 8;

        Assert.Equal(new TColorAttr(0x2), pair[0]);
        Assert.Equal(new TColorAttr(0), pair[1]);
    }

    [Fact]
    public void Should_ReplaceLow_When_OrAssigningOverZero()
    {
        var pair = new TAttrPair(0x0, 0x5);
        pair |= 0x7;

        Assert.Equal(new TColorAttr(0x7), pair[0]);
        Assert.Equal(new TColorAttr(0x5), pair[1]);
    }
}
