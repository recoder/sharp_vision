using System;

using SharpVision.Drawing;

namespace SharpVision.Views;

/// <summary>
/// A color palette. Entry 0 holds the length; entries 1 to length hold the colors.
/// </summary>
/// <remarks>
/// For regular views, the entries are 1-based indexes into the owner's palette
/// (<see cref="TColorAttr"/> only stores a number). For the application, they are actual
/// color attributes.
/// </remarks>
/// Ported from include/tvision/views.h and source/tvision/palette.cpp (class TPalette).
public sealed class TPalette
{
    /// <summary>The entries, including the length in entry 0.</summary>
    public TColorAttr[] Data { get; private set; }

    /// <summary>
    /// Creates a palette from bytes (upstream's <c>const char*</c> palettes such as
    /// <c>cpFrame</c>). Each byte becomes a BIOS attribute or palette index.
    /// </summary>
    public TPalette(ReadOnlySpan<byte> d)
    {
        Data = new TColorAttr[d.Length + 1];
        Data[0] = d.Length;
        for (var i = 0; i < d.Length; ++i)
            Data[i + 1] = d[i];
    }

    /// <summary>
    /// Creates a palette from color attributes. A factory rather than a constructor so that
    /// <c>new TPalette([0x01, 0x02])</c> is not ambiguous.
    /// </summary>
    public static TPalette FromAttributes(ReadOnlySpan<TColorAttr> d)
    {
        var data = new TColorAttr[d.Length + 1];
        data[0] = d.Length;
        d.CopyTo(data.AsSpan(1));
        return new TPalette(data);
    }

    private TPalette(TColorAttr[] data)
    {
        Data = data;
    }

    /// <summary>Creates a copy of <paramref name="tp"/>.</summary>
    public TPalette(TPalette tp)
    {
        Data = (TColorAttr[])tp.Data.Clone();
    }

    /// <summary>The number of entries, as stored in entry 0.</summary>
    public int Length => (byte)Data[0];

    /// <summary>Gets a reference to entry <paramref name="index"/> (0 is the length).</summary>
    public ref TColorAttr this[int index] => ref Data[index];

    /// <summary>Copies the entries of <paramref name="tp"/> into this palette (upstream's <c>operator=</c>).</summary>
    public void Assign(TPalette tp)
    {
        if (ReferenceEquals(Data, tp.Data))
            return;
        if (Data[0] != tp.Data[0])
        {
            Data = new TColorAttr[(byte)tp.Data[0] + 1];
            Data[0] = tp.Data[0];
        }
        Array.Copy(tp.Data, 1, Data, 1, (byte)Data[0]);
    }
}
