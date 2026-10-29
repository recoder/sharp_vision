// Ported from include/tvision/colors.h (class TColorRGB).

using System;

namespace SharpVision.Drawing;

/// <summary>
/// A 24-bit RGB color. Converts to and from <c>0xRRGGBB</c> integers; when converting
/// from an integer, the bits above the lower 24 are discarded.
/// </summary>
/// <remarks>magiblot/tvision extension (not part of Turbo Vision 2.0).</remarks>
public struct TColorRGB : IEquatable<TColorRGB>
{
    private uint _rgb;

    /// <summary>Creates a color from its components.</summary>
    public TColorRGB(byte r, byte g, byte b)
    {
        _rgb = (uint)((r << 16) | (g << 8) | b);
    }

    /// <summary>Creates a color from a <c>0xRRGGBB</c> integer. Bits 24 and above are discarded.</summary>
    public TColorRGB(uint rgb)
    {
        _rgb = rgb & 0xFFFFFF;
    }

    /// <summary>Red component.</summary>
    public byte Red
    {
        readonly get => (byte)(_rgb >> 16);
        set => _rgb = (_rgb & 0x00FFFF) | ((uint)value << 16);
    }

    /// <summary>Green component.</summary>
    public byte Green
    {
        readonly get => (byte)(_rgb >> 8);
        set => _rgb = (_rgb & 0xFF00FF) | ((uint)value << 8);
    }

    /// <summary>Blue component.</summary>
    public byte Blue
    {
        readonly get => (byte)_rgb;
        set => _rgb = (_rgb & 0xFFFF00) | value;
    }

    /// <summary>Converts a <c>0xRRGGBB</c> integer to a color.</summary>
    public static implicit operator TColorRGB(uint rgb) => new(rgb);

    /// <summary>Converts a <c>0xRRGGBB</c> integer to a color.</summary>
    public static implicit operator TColorRGB(int rgb) => new((uint)rgb);

    /// <summary>Converts the color to a <c>0xRRGGBB</c> integer.</summary>
    public static implicit operator uint(TColorRGB c) => c._rgb;

    /// <inheritdoc />
    public static bool operator ==(TColorRGB a, TColorRGB b) => a._rgb == b._rgb;

    /// <inheritdoc />
    public static bool operator !=(TColorRGB a, TColorRGB b) => a._rgb != b._rgb;

    /// <inheritdoc />
    public readonly bool Equals(TColorRGB other) => _rgb == other._rgb;

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is TColorRGB other && Equals(other);

    /// <inheritdoc />
    public override readonly int GetHashCode() => (int)_rgb;

    /// <inheritdoc />
    public override readonly string ToString() => $"#{_rgb:X6}";
}
