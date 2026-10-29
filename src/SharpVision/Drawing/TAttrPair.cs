// Ported from include/tvision/colors.h (class TAttrPair).

using System;

namespace SharpVision.Drawing;

/// <summary>
/// A pair of color attributes: index 0 is the normal attribute and index 1 the
/// highlight attribute (used e.g. by <c>TDrawBuffer.MoveCStr</c> for <c>~hotkey~</c> text).
/// </summary>
/// <remarks>
/// Converts implicitly from an <see cref="int"/> holding two BIOS attribute bytes
/// (low byte = normal, high byte = highlight). Note that, unlike upstream's default
/// constructor, <c>default(TAttrPair)</c> holds two <c>default(TColorAttr)</c> values.
/// </remarks>
public struct TAttrPair : IEquatable<TAttrPair>
{
    private TColorAttr _low;
    private TColorAttr _high;

    /// <summary>Creates a pair from two attributes.</summary>
    public TAttrPair(TColorAttr low, TColorAttr high)
    {
        _low = low;
        _high = high;
    }

    /// <summary>Creates a pair whose high attribute is BIOS 0x00.</summary>
    public TAttrPair(TColorAttr low)
        : this(low, 0)
    {
    }

    /// <summary>Creates a pair from two BIOS attribute bytes (low byte = index 0).</summary>
    public TAttrPair(int bios)
        : this(bios & 0xFF, (bios >> 8) & 0xFF)
    {
    }

    /// <summary>Gets or sets the attribute at <paramref name="index"/> (0 or 1).</summary>
    public TColorAttr this[int index]
    {
        readonly get => index switch
        {
            0 => _low,
            1 => _high,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
        set
        {
            switch (index)
            {
                case 0: _low = value; break;
                case 1: _high = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <summary>Converts two BIOS attribute bytes.</summary>
    public static implicit operator TAttrPair(int bios) => new(bios);

    /// <summary>Converts to two BIOS attribute bytes (see <c>explicit operator byte(TColorAttr)</c>).</summary>
    public static explicit operator ushort(TAttrPair pair) =>
        (ushort)((byte)pair._low | ((byte)pair._high << 8));

    /// <summary>
    /// <c>pair &gt;&gt; 8</c> yields a pair whose low attribute is the high attribute of
    /// <paramref name="pair"/>; other shifts operate on the BIOS bytes.
    /// </summary>
    public static TAttrPair operator >>(TAttrPair pair, int shift)
    {
        if (shift == 8)
            return new TAttrPair(pair._high);
        return new TAttrPair((ushort)pair >> shift);
    }

    /// <summary>
    /// Combines <paramref name="attr"/> into the low attribute: replaces it if it is
    /// BIOS 0x00, otherwise ORs the BIOS bytes. Enables <c>pair |= attr</c>.
    /// </summary>
    public static TAttrPair operator |(TAttrPair pair, TColorAttr attr)
    {
        if (pair._low == 0)
            pair._low = attr;
        else
            pair._low = (byte)pair._low | (byte)attr;
        return pair;
    }

    /// <inheritdoc />
    public static bool operator ==(TAttrPair a, TAttrPair b) => a.Equals(b);

    /// <inheritdoc />
    public static bool operator !=(TAttrPair a, TAttrPair b) => !a.Equals(b);

    /// <inheritdoc />
    public readonly bool Equals(TAttrPair other) => _low == other._low && _high == other._high;

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is TAttrPair other && Equals(other);

    /// <inheritdoc />
    public override readonly int GetHashCode() => HashCode.Combine(_low, _high);

    /// <inheritdoc />
    public override readonly string ToString() => $"[{_low}, {_high}]";
}
