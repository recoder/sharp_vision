// Ported from include/tvision/colors.h (class TColor, TColorDefault) and
// source/platform/colors.cpp (TColor::toBIOS).

using System;

namespace SharpVision.Drawing;

/// <summary>
/// A color in any of the supported encodings (BIOS, RGB, XTerm) or the terminal
/// default color. <c>default(TColor)</c> is the terminal default color.
/// </summary>
/// <remarks>magiblot/tvision extension (not part of Turbo Vision 2.0).</remarks>
public readonly struct TColor : IEquatable<TColor>
{
    private const uint CtDefault = 0x0;
    private const uint CtBIOS = 0x1;
    private const uint CtRGB = 0x2;
    private const uint CtXTerm = 0x3;

    // Bits 0-23 hold the color, bits 24-25 its type. Shared with TColorAttr's bit layout.
    internal readonly uint Data;

    internal TColor(uint data)
    {
        Data = data;
    }

    /// <summary>The terminal default color (equivalent to upstream's <c>TColorDefault</c>).</summary>
    public static TColor Default => default;

    /// <summary>Creates a BIOS color. Bits 4 and above are discarded.</summary>
    public static TColor FromBIOS(byte bios) => new((uint)(bios & 0xF) | (CtBIOS << 24));

    /// <summary>Creates an RGB color from <c>0xRRGGBB</c>. Bits 24 and above are discarded.</summary>
    public static TColor FromRGB(int rgb) => new(((uint)rgb & 0xFFFFFF) | (CtRGB << 24));

    /// <summary>Creates an XTerm palette color.</summary>
    public static TColor FromXTerm(byte idx) => new(idx | (CtXTerm << 24));

    private uint Type => Data >> 24;

    /// <summary>Whether this is the terminal default color.</summary>
    public bool IsDefault => Type == CtDefault;

    /// <summary>Whether this is a BIOS color.</summary>
    public bool IsBIOS => Type == CtBIOS;

    /// <summary>Whether this is an RGB color.</summary>
    public bool IsRGB => Type == CtRGB;

    /// <summary>Whether this is an XTerm palette color.</summary>
    public bool IsXTerm => Type == CtXTerm;

    /// <summary>Reinterprets the color as BIOS. Only meaningful when <see cref="IsBIOS"/>.</summary>
    public TColorBIOS AsBIOS() => (byte)Data;

    /// <summary>Reinterprets the color as RGB. Only meaningful when <see cref="IsRGB"/>.</summary>
    public TColorRGB AsRGB() => new(Data);

    /// <summary>Reinterprets the color as XTerm. Only meaningful when <see cref="IsXTerm"/>.</summary>
    public TColorXTerm AsXTerm() => (byte)Data;

    /// <summary>
    /// Converts the color to the closest BIOS color. The default color becomes gray
    /// (0x7) as a foreground and black (0x0) as a background.
    /// </summary>
    public TColorBIOS ToBIOS(bool isForeground)
    {
        switch (Type)
        {
            case CtBIOS:
                return AsBIOS();
            case CtRGB:
                return TColorConversion.XTerm16toBIOS(TColorConversion.RGBtoXTerm16(AsRGB()));
            case CtXTerm:
                {
                    var idx = AsXTerm();
                    if (idx >= 16)
                        idx = TColorConversion.XTerm256toXTerm16(idx);
                    return TColorConversion.XTerm16toBIOS(idx);
                }
            default:
                return (byte)(isForeground ? 0x7 : 0x0);
        }
    }

    /// <summary>Converts a BIOS color.</summary>
    public static implicit operator TColor(TColorBIOS bios) => FromBIOS(bios);

    /// <summary>Converts an RGB color.</summary>
    public static implicit operator TColor(TColorRGB rgb) => FromRGB((int)(uint)rgb);

    /// <summary>Converts an XTerm palette color.</summary>
    public static implicit operator TColor(TColorXTerm xterm) => FromXTerm(xterm);

    /// <inheritdoc />
    public static bool operator ==(TColor a, TColor b) => a.Data == b.Data;

    /// <inheritdoc />
    public static bool operator !=(TColor a, TColor b) => a.Data != b.Data;

    /// <inheritdoc />
    public bool Equals(TColor other) => Data == other.Data;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TColor other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => (int)Data;

    /// <inheritdoc />
    public override string ToString() => Type switch
    {
        CtBIOS => AsBIOS().ToString(),
        CtRGB => AsRGB().ToString(),
        CtXTerm => AsXTerm().ToString(),
        _ => "Default",
    };
}
