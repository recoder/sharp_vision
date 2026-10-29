// Ported from include/tvision/drawbuf.h and source/tvision/drivers.cpp (class TDrawBuffer).

using System;

using SharpVision.Platform;

namespace SharpVision.Drawing;

/// <summary>
/// A row of screen cells that views fill in before writing them to the screen with
/// <c>TView.WriteLine</c> or <c>TView.WriteBuf</c>.
/// </summary>
/// <remarks>
/// Positions and widths are in columns. Upstream takes them as <see cref="ushort"/>; the
/// <see cref="int"/> arguments here are narrowed the same way, so e.g. a negative indent
/// wraps around to a large one and nothing is drawn.
/// </remarks>
public class TDrawBuffer
{
    /// <summary>The cells. Its length is the buffer's capacity.</summary>
    protected internal TScreenCell[] Data { get; }

    /// <summary>
    /// Creates a buffer as wide as the larger screen dimension (views such as scroll bars
    /// draw vertically), with room to spare for views that exceed the screen. The cells
    /// start empty, with default attributes.
    /// </summary>
    public TDrawBuffer()
    {
        var capacity = 8 + Math.Max(Math.Max((int)TScreen.ScreenWidth, TScreen.ScreenHeight), 80);
        Data = new TScreenCell[capacity];
    }

    /// <summary>The number of cells in the buffer.</summary>
    protected internal int Capacity => Data.Length;

    /// <summary>
    /// Fills <paramref name="count"/> cells starting at <paramref name="indent"/>. A
    /// <paramref name="c"/> of <c>'\0'</c> keeps the existing characters and an
    /// <paramref name="attr"/> of 0 keeps the existing attributes. If both are 0, the
    /// attributes are kept and the characters are cleared.
    /// </summary>
    public void MoveChar(int indent, char c, TColorAttr attr, int count)
    {
        indent = (ushort)indent;
        count = (ushort)count;
        if (count == 0 || indent >= Capacity)
            return;
        if (indent + count >= Capacity)
            count = Capacity - indent;

        var dest = Data.AsSpan(indent, count);
        if (attr != 0)
        {
            if (c != 0)
                dest.Fill(new TScreenCell(c, attr));
            else
            {
                foreach (ref var cell in dest)
                    cell.Attribute = attr;
            }
        }
        else
        {
            foreach (ref var cell in dest)
                cell.Character = c;
        }
    }

    /// <summary>
    /// Copies <paramref name="str"/>, starting at column <paramref name="strIndent"/>, into the
    /// buffer at <paramref name="indent"/>, at most <paramref name="maxStrWidth"/> columns wide.
    /// An <paramref name="attr"/> of 0 keeps the existing attributes.
    /// </summary>
    /// <returns>The width of the copied text, in columns.</returns>
    public int MoveStr(int indent, ReadOnlySpan<char> str, TColorAttr attr, int maxStrWidth = ushort.MaxValue, int strIndent = 0)
    {
        indent = (ushort)indent;
        maxStrWidth = (ushort)maxStrWidth;
        strIndent = (ushort)strIndent;
        if (indent >= Capacity || str.Length == 0 || maxStrWidth == 0)
            return 0;
        if (indent + maxStrWidth >= Capacity)
            maxStrWidth = Capacity - indent;

        var dest = Data.AsSpan(0, indent + maxStrWidth);
        if (attr != 0)
            return TText.DrawStr(dest, indent, str, strIndent, attr);
        return TText.DrawStr(dest, indent, str, strIndent);
    }

    /// <summary>
    /// Like <see cref="MoveStr"/>, but each <c>'~'</c> in <paramref name="str"/> toggles between
    /// <c>attrs[0]</c> (initially) and <c>attrs[1]</c> and is not drawn. Used for hotkey text
    /// such as <c>"~F~ile"</c>.
    /// </summary>
    /// <returns>The width of the copied text, in columns.</returns>
    public int MoveCStr(int indent, ReadOnlySpan<char> str, TAttrPair attrs, int maxStrWidth = ushort.MaxValue, int strIndent = 0)
    {
        indent = (ushort)indent;
        maxStrWidth = (ushort)maxStrWidth;
        strIndent = (ushort)strIndent;
        if (indent >= Capacity || str.Length == 0 || maxStrWidth == 0)
            return 0;
        if (indent + maxStrWidth >= Capacity)
            maxStrWidth = Capacity - indent;

        int i = indent, j = 0, w = 0;
        var toggle = 1;
        var curAttr = attrs[0];
        var dest = Data.AsSpan(0, indent + maxStrWidth);
        while (j < str.Length)
        {
            if (str[j] == '~')
            {
                curAttr = attrs[toggle];
                toggle = 1 - toggle;
                ++j;
            }
            else if (strIndent <= w)
            {
                if (!TText.DrawOne(dest, ref i, str, ref j, curAttr))
                    break;
            }
            else
            {
                if (!TText.Next(str, ref j, ref w))
                    break;
                if (strIndent < w && i < dest.Length)
                {
                    // 'strIndent' is in the middle of a double-width character.
                    dest[i++] = new TScreenCell(' ', curAttr);
                }
            }
        }
        return i - indent;
    }

    /// <summary>
    /// Copies the first <paramref name="count"/> characters of <paramref name="source"/> into
    /// the buffer at <paramref name="indent"/>. An <paramref name="attr"/> of 0 keeps the
    /// existing attributes.
    /// </summary>
    public void MoveBuf(int indent, ReadOnlySpan<char> source, TColorAttr attr, int count)
    {
        count = Math.Min((ushort)count, source.Length);
        MoveStr(indent, source[..count], attr);
    }

    /// <summary>Sets the attribute of the cell at <paramref name="indent"/>.</summary>
    public void PutAttribute(int indent, TColorAttr attr)
    {
        indent = (ushort)indent;
        if (indent < Capacity)
            Data[indent].Attribute = attr;
    }

    /// <summary>Sets the character of the cell at <paramref name="indent"/>.</summary>
    public void PutChar(int indent, char c)
    {
        indent = (ushort)indent;
        if (indent < Capacity)
            Data[indent].Character = c;
    }
}
