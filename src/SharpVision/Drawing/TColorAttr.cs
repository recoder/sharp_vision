// Ported from include/tvision/colors.h (struct TColorAttr) and
// source/platform/colors.cpp (TColorAttr::toBIOS).

using System;

using static SharpVision.Drawing.StyleFlags;

namespace SharpVision.Drawing;

/// <summary>
/// The foreground color, background color and style used when writing text to the
/// screen. <c>default(TColorAttr)</c> has default colors and no style.
/// </summary>
/// <remarks>
/// Converts implicitly from an <see cref="int"/> holding a classic BIOS attribute byte
/// (low nibble foreground, high nibble background), so Turbo Vision palette values such
/// as <c>0x1F</c> can be used directly.
/// </remarks>
public struct TColorAttr : IEquatable<TColorAttr>
{
    private const ulong FgMask = (1UL << 27) - 1;
    private const ulong BgMask = (1UL << 27) - 1;
    private const ulong StyleMask = (1UL << 10) - 1;

    private ulong _data;

    /// <summary>Creates an attribute from a BIOS attribute byte (bits 8 and above are ignored).</summary>
    public TColorAttr(int bios)
        : this(TColor.FromBIOS((byte)bios), TColor.FromBIOS((byte)(bios >> 4)))
    {
    }

    /// <summary>Creates an attribute from colors and a style (<see cref="StyleFlags"/>).</summary>
    public TColorAttr(TColor fg, TColor bg, ushort style = 0)
    {
        _data = (fg.Data & FgMask)
            | ((bg.Data & BgMask) << 27)
            | ((ulong)style << 54);
    }

    /// <summary>Creates an attribute from the low attribute of a pair.</summary>
    public TColorAttr(TAttrPair attrs)
        : this()
    {
        this = attrs[0];
    }

    /// <summary>Foreground color.</summary>
    public TColor Foreground
    {
        readonly get => new((uint)(_data & FgMask));
        set => _data = (_data & ~FgMask) | (value.Data & FgMask);
    }

    /// <summary>Background color.</summary>
    public TColor Background
    {
        readonly get => new((uint)((_data >> 27) & BgMask));
        set => _data = (_data & ~(BgMask << 27)) | ((value.Data & BgMask) << 27);
    }

    /// <summary>Style flags (<see cref="StyleFlags"/>).</summary>
    public ushort Style
    {
        readonly get => (ushort)(_data >> 54);
        set => _data = (_data & ~(StyleMask << 54)) | ((ulong)value << 54);
    }

    /// <summary>
    /// Returns the attribute with foreground and background swapped. If either color is
    /// the default color, toggles <see cref="slReverse"/> instead.
    /// </summary>
    public readonly TColorAttr Reversed()
    {
        var fg = Foreground;
        var bg = Background;
        if (fg.IsDefault || bg.IsDefault)
            return new TColorAttr(fg, bg, (ushort)(Style ^ slReverse));
        return new TColorAttr(bg, fg, Style);
    }

    /// <summary>Converts the attribute to the closest BIOS attribute byte. Style is ignored.</summary>
    public readonly byte ToBIOS() =>
        (byte)(Foreground.ToBIOS(true) | (Background.ToBIOS(false) << 4));

    /// <summary>Converts a BIOS attribute byte.</summary>
    public static implicit operator TColorAttr(int bios) => new(bios);

    /// <summary>Takes the low attribute of a pair.</summary>
    public static implicit operator TColorAttr(TAttrPair attrs) => attrs[0];

    /// <summary>
    /// Returns the BIOS attribute byte if both colors are BIOS and there is no style;
    /// otherwise 0x5F.
    /// </summary>
    public static explicit operator byte(TColorAttr attr)
    {
        var fg = attr.Foreground;
        var bg = attr.Background;
        if (fg.IsBIOS && bg.IsBIOS && attr.Style == 0)
            return (byte)(fg.AsBIOS() | (bg.AsBIOS() << 4));
        return 0x5F;
    }

    /// <summary>
    /// <c>attr &lt;&lt; 8</c> yields a pair whose high attribute is <paramref name="attr"/>;
    /// other shifts operate on the BIOS byte.
    /// </summary>
    public static TAttrPair operator <<(TColorAttr attr, int shift)
    {
        if (shift == 8)
            return new TAttrPair(0, attr);
        return new TAttrPair((byte)attr << shift);
    }

    /// <inheritdoc />
    public static bool operator ==(TColorAttr a, TColorAttr b) => a._data == b._data;

    /// <inheritdoc />
    public static bool operator !=(TColorAttr a, TColorAttr b) => a._data != b._data;

    /// <inheritdoc />
    public readonly bool Equals(TColorAttr other) => _data == other._data;

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is TColorAttr other && Equals(other);

    /// <inheritdoc />
    public override readonly int GetHashCode() => _data.GetHashCode();

    /// <inheritdoc />
    public override readonly string ToString() => $"{{fg: {Foreground}, bg: {Background}, style: 0x{Style:X}}}";
}
