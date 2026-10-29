// Ported from include/tvision/ttext.h and source/platform/ttext.cpp (class TText).

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

using SharpVision.Platform;

namespace SharpVision.Drawing;

/// <summary>
/// Measures text in screen columns and draws it into screen cells.
/// </summary>
/// <remarks>
/// <para>
/// UPSTREAM: upstream works on UTF-8 bytes; this port works on UTF-16 strings, so every
/// index and length is in UTF-16 code units and a "character" is a <see cref="Rune"/>
/// (one code unit, or two for a surrogate pair). ASCII control characters are drawn as
/// their CP437 glyphs, as upstream does. Unpaired surrogates and other non-printable
/// characters are drawn as U+FFFD.
/// </para>
/// <para>
/// The UTF-32 overloads of upstream are not ported: <see cref="string"/> covers them.
/// </para>
/// </remarks>
public static class TText
{
    /// <summary>Callback that changes the color attribute of a cell in place.</summary>
    public delegate void AttrTransform(ref TColorAttr attr);

    private const char ReplacementChar = '�';

    private readonly record struct Lw(int Length, int Width);

    /// <summary>Returns the width of <paramref name="text"/> in columns.</summary>
    public static int Width(ReadOnlySpan<char> text)
    {
        int i = 0, width = 0;
        while (Next(text, ref i, ref width))
        {
        }
        return width;
    }

    /// <summary>Returns the width, character count and grapheme count (non-combining characters) of <paramref name="text"/>.</summary>
    public static TTextMetrics Measure(ReadOnlySpan<char> text)
    {
        int i = 0, totalWidth = 0, characterCount = 0, graphemeCount = 0;
        while (true)
        {
            var width = 0;
            if (!Next(text, ref i, ref width))
                break;
            totalWidth += width;
            characterCount += 1;
            graphemeCount += width > 0 ? 1 : 0;
        }
        return new TTextMetrics(totalWidth, characterCount, graphemeCount);
    }

    /// <summary>Returns the length in code units of the first character in <paramref name="text"/>.</summary>
    public static int Next(ReadOnlySpan<char> text)
    {
        if (text.Length == 0)
            return 0;
        return Rune.DecodeFromUtf16(text, out _, out var consumed) == OperationStatus.Done ? consumed : 1;
    }

    /// <summary>
    /// Advances <paramref name="index"/> past the character starting at it.
    /// Returns whether <paramref name="index"/> was increased at all.
    /// </summary>
    public static bool Next(ReadOnlySpan<char> text, ref int index)
    {
        var length = Next(Tail(text, index));
        index += length;
        return length != 0;
    }

    /// <summary>
    /// Advances <paramref name="index"/> past the character starting at it and increases
    /// <paramref name="width"/> by the character's width.
    /// Returns whether <paramref name="index"/> was increased at all.
    /// </summary>
    public static bool Next(ReadOnlySpan<char> text, ref int index, ref int width)
    {
        var result = NextImpl(Tail(text, index));
        index += result.Length;
        width += result.Width;
        return result.Length != 0;
    }

    /// <summary>Returns the length in code units of the character right before position <paramref name="index"/>.</summary>
    public static int Prev(ReadOnlySpan<char> text, int index)
    {
        if (index <= 0)
            return 0;
        if (index >= 2 && index <= text.Length && char.IsLowSurrogate(text[index - 1]) && char.IsHighSurrogate(text[index - 2]))
            return 2;
        return 1;
    }

    /// <summary>Compares two strings ignoring case.</summary>
    public static bool EqualsIgnoreCase(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        while (!a.IsEmpty && !b.IsEmpty)
        {
            var lenA = DecodeFirst(a, out var wcA);
            var lenB = DecodeFirst(b, out var wcB);
            // Converting both characters to lowercase is the recommended way of doing
            // case-insensitive comparison.
            if (CharOps.ToLower(wcA) != CharOps.ToLower(wcB))
                break;
            a = a[lenA..];
            b = b[lenB..];
        }
        return a.IsEmpty && b.IsEmpty;
    }

    /// <summary>
    /// Converts the first character in <paramref name="text"/> to a code page character.
    /// Returns 0 if it cannot be converted or <paramref name="text"/> is empty.
    /// </summary>
    public static byte ToCodePage(ReadOnlySpan<char> text)
    {
        var length = Next(text);
        // ASCII characters must not be converted, let them through.
        if (length == 1 && text[0] <= 0x7F)
            return (byte)text[0];
        return CpTranslator.FromUnicode(text[..length]);
    }

    /// <summary>Converts the code page character <paramref name="c"/> to Unicode.</summary>
    public static string FromCodePage(byte c) => CpTranslator.ToUnicode(c);

    /// <summary>
    /// Replaces the 256-entry table used to convert code page characters to and from
    /// Unicode. By default, code page 437 (United States) is used. Null restores it.
    /// </summary>
    public static void SetCodePageTranslation(IReadOnlyList<string?>? translation) =>
        CpTranslator.SetTranslation(translation);

    /// <summary>
    /// Returns the length in code units of the leading substring of <paramref name="text"/>
    /// that is <paramref name="count"/> columns wide, or <c>text.Length</c> if the text is
    /// narrower. Negative counts are treated as 0. When column <paramref name="count"/> falls
    /// in the middle of a double-width character, <paramref name="includeIncomplete"/>
    /// decides whether that character is included.
    /// </summary>
    public static int Scroll(ReadOnlySpan<char> text, int count, bool includeIncomplete)
    {
        Scroll(text, count, includeIncomplete, out var length, out _);
        return length;
    }

    /// <summary>
    /// Like <see cref="Scroll(ReadOnlySpan{char}, int, bool)"/>, also returning the width
    /// of the substring.
    /// </summary>
    public static void Scroll(ReadOnlySpan<char> text, int count, bool includeIncomplete, out int length, out int width)
    {
        length = 0;
        width = 0;
        if (count > 0)
        {
            int i = 0, w = 0;
            while (true)
            {
                int i2 = i, w2 = w;
                if (!Next(text, ref i, ref w) || w == count)
                    break;
                if (w > count)
                {
                    if (!includeIncomplete)
                    {
                        i = i2;
                        w = w2;
                    }
                    break;
                }
            }
            length = i;
            width = w;
        }
    }

    /// <summary>Fills <paramref name="cells"/> with the character <paramref name="c"/>, keeping their attributes.</summary>
    public static void DrawChar(Span<TScreenCell> cells, char c)
    {
        foreach (ref var cell in cells)
            cell.Character = c;
    }

    /// <summary>Fills <paramref name="cells"/> with the character <paramref name="c"/> and attribute <paramref name="attr"/>.</summary>
    public static void DrawChar(Span<TScreenCell> cells, char c, TColorAttr attr) =>
        cells.Fill(new TScreenCell(c, attr));

    /// <summary>
    /// Fills <paramref name="cells"/> with the character <paramref name="c"/> and changes
    /// each cell's attribute with <paramref name="transformAttr"/>.
    /// </summary>
    public static void DrawCharEx(Span<TScreenCell> cells, char c, AttrTransform transformAttr)
    {
        foreach (ref var cell in cells)
        {
            cell.Character = c;
            transformAttr(ref cell.Attribute);
        }
    }

    /// <summary>Copies <paramref name="text"/> into <paramref name="cells"/>, keeping their attributes.</summary>
    /// <returns>The number of cells filled.</returns>
    public static int DrawStr(Span<TScreenCell> cells, ReadOnlySpan<char> text) =>
        DrawStr(cells, 0, text, 0);

    /// <summary>Copies <paramref name="text"/> into <paramref name="cells"/> with attribute <paramref name="attr"/>.</summary>
    /// <returns>The number of cells filled.</returns>
    public static int DrawStr(Span<TScreenCell> cells, ReadOnlySpan<char> text, TColorAttr attr) =>
        DrawStr(cells, 0, text, 0, attr);

    /// <summary>
    /// Copies <paramref name="text"/>, starting at column <paramref name="textIndent"/>, into
    /// <paramref name="cells"/> starting at position <paramref name="indent"/>, keeping the cells'
    /// attributes. When column <paramref name="textIndent"/> falls in the middle of a
    /// double-width character, a space is copied in its place.
    /// </summary>
    /// <returns>The number of cells filled.</returns>
    public static int DrawStr(Span<TScreenCell> cells, int indent, ReadOnlySpan<char> text, int textIndent) =>
        DrawStrExT(cells, indent, text, textIndent, default(KeepAttr));

    /// <summary>
    /// Like <see cref="DrawStr(Span{TScreenCell}, int, ReadOnlySpan{char}, int)"/>, also setting
    /// the attribute of each filled cell to <paramref name="attr"/>.
    /// </summary>
    /// <returns>The number of cells filled.</returns>
    public static int DrawStr(Span<TScreenCell> cells, int indent, ReadOnlySpan<char> text, int textIndent, TColorAttr attr) =>
        DrawStrExT(cells, indent, text, textIndent, new SetAttr(attr));

    /// <summary>
    /// Like <see cref="DrawStr(Span{TScreenCell}, int, ReadOnlySpan{char}, int)"/>, changing the
    /// attribute of each filled cell with <paramref name="transformAttr"/>.
    /// </summary>
    /// <returns>The number of cells filled.</returns>
    public static int DrawStrEx(Span<TScreenCell> cells, int indent, ReadOnlySpan<char> text, int textIndent, AttrTransform transformAttr) =>
        DrawStrExT(cells, indent, text, textIndent, new DelegateAttr(transformAttr));

    /// <summary>
    /// Reads one character from <paramref name="text"/> at <paramref name="j"/> and writes it
    /// into <paramref name="cells"/> at <paramref name="i"/>, keeping the cell's attribute.
    /// <paramref name="i"/> advances by the cells filled and <paramref name="j"/> by the code
    /// units read. A zero-width character is appended to the previous cell instead, without
    /// advancing <paramref name="i"/>.
    /// </summary>
    /// <returns>False once there is nothing more to read or no room to write it.</returns>
    public static bool DrawOne(Span<TScreenCell> cells, ref int i, ReadOnlySpan<char> text, ref int j) =>
        DrawOneT(cells, ref i, text, ref j, default(KeepAttr));

    /// <summary>
    /// Like <see cref="DrawOne(Span{TScreenCell}, ref int, ReadOnlySpan{char}, ref int)"/>, also
    /// setting the attribute of the filled cells to <paramref name="attr"/>.
    /// </summary>
    /// <returns>False once there is nothing more to read or no room to write it.</returns>
    public static bool DrawOne(Span<TScreenCell> cells, ref int i, ReadOnlySpan<char> text, ref int j, TColorAttr attr) =>
        DrawOneT(cells, ref i, text, ref j, new SetAttr(attr));

    private interface IAttrTransform
    {
        void Apply(ref TColorAttr attr);
    }

    private readonly struct KeepAttr : IAttrTransform
    {
        public void Apply(ref TColorAttr attr)
        {
        }
    }

    private readonly struct SetAttr(TColorAttr value) : IAttrTransform
    {
        public void Apply(ref TColorAttr attr) => attr = value;
    }

    private readonly struct DelegateAttr(AttrTransform transform) : IAttrTransform
    {
        public void Apply(ref TColorAttr attr) => transform(ref attr);
    }

    private static int DrawStrExT<T>(Span<TScreenCell> cells, int indent, ReadOnlySpan<char> text, int textIndent, T transformAttr)
        where T : struct, IAttrTransform
    {
        int i = indent, j = 0;
        if (textIndent > 0)
        {
            Scroll(text, textIndent, true, out j, out var leadWidth);
            if (leadWidth > textIndent && i < cells.Length)
            {
                cells[i].Character = ' ';
                transformAttr.Apply(ref cells[i++].Attribute);
            }
        }
        while (DrawOneT(cells, ref i, text, ref j, transformAttr))
        {
        }
        return i - indent;
    }

    private static bool DrawOneT<T>(Span<TScreenCell> cells, ref int i, ReadOnlySpan<char> text, ref int j, T transformAttr)
        where T : struct, IAttrTransform
    {
        var result = DrawOneImpl(cells, i, text, j);
        if (result.Width != 0)
            transformAttr.Apply(ref cells[i].Attribute);
        if (result.Width > 1)
            transformAttr.Apply(ref cells[i + 1].Attribute);
        i += result.Width;
        j += result.Length;
        return result.Length != 0;
    }

    private static Lw DrawOneImpl(Span<TScreenCell> cells, int i, ReadOnlySpan<char> text, int j)
    {
        if (j < text.Length)
        {
            var c = text[j];
            if (c < 0x80)
            {
                if (i < cells.Length)
                {
                    // Control characters are converted here since combining characters
                    // might later be appended to them.
                    if (c < ' ' || c == '\x7F')
                        cells[i].Character.InitWithText(CpTranslator.ToUnicode((byte)c));
                    else
                        cells[i].Character.InitWithChar(c);
                    return new Lw(1, 1);
                }
            }
            else
            {
                var (length, width) = MbStat(text[j..]);
                if (width < 0)
                {
                    if (i < cells.Length)
                    {
                        cells[i].Character.InitWithChar(ReplacementChar);
                        return new Lw(length, 1);
                    }
                }
                else if (width == 0)
                {
                    var zwc = text.Slice(j, length);
                    // Append to the previous cell, if present. The 'i <= cells.Length' check
                    // only guards against an out-of-range 'i', which upstream does not check.
                    if (i > 0 && i <= cells.Length && !IsZeroWidthJoiner(zwc))
                    {
                        var k = i;
                        while (cells[--k].Character.IsWideCharTrail && k > 0)
                        {
                        }
                        cells[k].Character.AppendZeroWidthChar(zwc);
                    }
                    return new Lw(length, 0);
                }
                else if (i < cells.Length)
                {
                    var wide = width > 1;
                    cells[i].Character.InitWithText(text.Slice(j, length), wide);
                    var drawTrail = wide && i + 1 < cells.Length;
                    if (drawTrail)
                        cells[i + 1].Character.InitAsWideCharTrail();
                    return new Lw(length, drawTrail ? 2 : 1);
                }
            }
        }
        return new Lw(0, 0);
    }

    private static Lw NextImpl(ReadOnlySpan<char> text)
    {
        if (text.Length == 0)
            return new Lw(0, 0);
        if (text[0] < 0x80)
            return new Lw(1, 1);
        var (length, width) = MbStat(text);
        return new Lw(length, width == 0 ? 0 : Math.Max(width, 1));
    }

    // Pre: text.Length > 0. Width is -1 for unpaired surrogates and non-printable characters.
    private static (int Length, int Width) MbStat(ReadOnlySpan<char> text)
    {
        if (Rune.DecodeFromUtf16(text, out var rune, out var consumed) != OperationStatus.Done)
            return (1, -1);
        return (consumed, CharOps.Width(rune.Value));
    }

    // Pre: text.Length > 0. Unpaired surrogates are returned as is.
    private static int DecodeFirst(ReadOnlySpan<char> text, out int codePoint)
    {
        if (Rune.DecodeFromUtf16(text, out var rune, out var consumed) == OperationStatus.Done)
        {
            codePoint = rune.Value;
            return consumed;
        }
        codePoint = text[0];
        return 1;
    }

    // Characters that terminals render inconsistently or that can combine others,
    // changing the width of a whole string, are not printed.
    private static bool IsZeroWidthJoiner(ReadOnlySpan<char> mbc) => mbc is "‍";

    private static ReadOnlySpan<char> Tail(ReadOnlySpan<char> text, int index) =>
        (uint)index < (uint)text.Length ? text[index..] : [];
}
