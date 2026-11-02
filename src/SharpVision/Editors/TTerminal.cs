using System;

using SharpVision.Drawing;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Views.ViewFlags;

namespace SharpVision.Editors;

/// <summary>
/// A text device that keeps the last lines written in a circular buffer and shows them, scrolled to the end,
/// with the cursor after the last character.
/// </summary>
/// Ported from source/tvision/textview.cpp, source/tvision/ttprvlns.cpp,
/// include/tvision/textview.h (class TTerminal).
/// <remarks>The buffer holds UTF-16 code units, so its size counts code units instead of bytes.</remarks>
public class TTerminal : TTextDevice
{
    /// <summary>The size of the buffer.</summary>
    protected int BufSize;
    /// <summary>The circular buffer; the text runs from <see cref="QueBack"/> to <see cref="QueFront"/>.</summary>
    protected char[] Buffer;
    /// <summary>Where the next character goes.</summary>
    protected int QueFront;
    /// <summary>The oldest character kept.</summary>
    protected int QueBack;

    /// <summary>Creates a terminal keeping up to <paramref name="aBufSize"/> characters (at most 32000).</summary>
    public TTerminal(TRect bounds, TScrollBar? aHScrollBar, TScrollBar? aVScrollBar, int aBufSize)
        : base(bounds, aHScrollBar, aVScrollBar)
    {
        GrowMode = gfGrowHiX + gfGrowHiY;
        BufSize = Math.Min(32000, (int)(ushort)aBufSize);
        Buffer = new char[BufSize];
        SetLimit(0, 1);
        SetCursor(0, 0);
        ShowCursor();
    }

    /// <summary>Moves <paramref name="val"/> one position back, wrapping around.</summary>
    protected void BufDec(ref int val)
    {
        if (val == 0)
            val = BufSize - 1;
        else
            val--;
    }

    /// <summary>Moves <paramref name="val"/> one position forward, wrapping around.</summary>
    public void BufInc(ref int val)
    {
        if (++val >= BufSize)
            val = 0;
    }

    /// <summary>Whether <paramref name="amount"/> characters fit without dropping old ones.</summary>
    public bool CanInsert(int amount)
    {
        long t = QueFront < QueBack ? QueFront + amount : (long)QueFront - BufSize + amount;
        return QueBack > t;
    }

    /// <summary>Draws the lines that end at the bottom of the view, and puts the cursor after the last character.</summary>
    public override void Draw()
    {
        var b = new TDrawBuffer();
        Span<char> s = stackalloc char[256];
        var color = MapColor(1);

        SetCursor(-1, -1);

        int endLine;
        var bottomLine = Size.Y + Delta.Y;
        if (Limit.Y > bottomLine)
        {
            endLine = PrevLines(QueFront, Limit.Y - bottomLine);
            BufDec(ref endLine);
        }
        else
            endLine = QueFront;

        int y;
        if (Limit.Y > Size.Y)
            y = Size.Y - 1;
        else
        {
            b.MoveChar(0, ' ', color, Size.X);
            WriteLine(0, Limit.Y, Size.X, Size.Y - Limit.Y, b);
            y = Limit.Y - 1;
        }

        for (; y >= 0; y--)
        {
            var x = 0;
            var begLine = PrevLines(endLine, 1);
            var linePos = begLine;
            while (linePos != endLine)
            {
                int sLen;
                if (endLine >= linePos)
                {
                    sLen = Math.Min(endLine - linePos, s.Length);
                    Buffer.AsSpan(linePos, sLen).CopyTo(s);
                }
                else
                {
                    var fstCpyLen = Math.Min(BufSize - linePos, s.Length);
                    var sndCpyLen = Math.Min(endLine, s.Length - fstCpyLen);
                    Buffer.AsSpan(linePos, fstCpyLen).CopyTo(s);
                    Buffer.AsSpan(0, sndCpyLen).CopyTo(s[fstCpyLen..]);
                    sLen = fstCpyLen + sndCpyLen;
                }

                // Do not split a surrogate pair at the end of a full chunk.
                if (sLen == s.Length && char.IsHighSurrogate(s[sLen - 1]))
                    sLen--;
                if (linePos >= BufSize - sLen)
                    linePos = sLen - (BufSize - linePos);
                else
                    linePos += sLen;

                x += b.MoveStr(x, s[..sLen], color);
            }

            b.MoveChar(x, ' ', color, Math.Max(Size.X - x, 0));
            WriteBuf(0, y, Size.X, 1, b);
            // Draw the cursor when this is the last line.
            if (endLine == QueFront)
                SetCursor(x, y);
            endLine = begLine;
            BufDec(ref endLine);
        }
    }

    /// <summary>The start of the line after the one at <paramref name="pos"/>, or <see cref="QueFront"/>.</summary>
    public int NextLine(int pos)
    {
        while (pos != QueFront && Buffer[pos] != '\n')
            BufInc(ref pos);
        if (pos != QueFront)
            BufInc(ref pos);
        return pos;
    }

    /// <summary>Appends <paramref name="s"/> (only its tail if longer than the buffer), dropping the oldest lines to make room, and scrolls to the end.</summary>
    public override int DoSputn(ReadOnlySpan<char> s)
    {
        var screenLines = Limit.Y;

        if (s.Length > BufSize - 1)
            s = s[^(BufSize - 1)..];
        var count = s.Length;

        foreach (var c in s)
        {
            if (c == '\n')
                screenLines++;
        }

        while (!CanInsert(count))
        {
            QueBack = NextLine(QueBack);
            if (screenLines > 1)
                screenLines--;
        }

        if (QueFront + count >= BufSize)
        {
            var i = BufSize - QueFront;
            s[..i].CopyTo(Buffer.AsSpan(QueFront));
            s[i..].CopyTo(Buffer);
            QueFront = count - i;
        }
        else
        {
            s.CopyTo(Buffer.AsSpan(QueFront));
            QueFront += count;
        }

        // drawLock: avoid redundant calls to drawView()
        DrawLock++;
        SetLimit(Limit.X, screenLines);
        ScrollTo(0, screenLines + 1);
        DrawLock--;

        DrawView();
        return count;
    }

    /// <summary>The start of the line <paramref name="lines"/> lines before the one at <paramref name="pos"/>, stopping at <see cref="QueBack"/>.</summary>
    public int PrevLines(int pos, int lines)
    {
        if (lines > 0 && pos != QueBack)
        {
            do
            {
                if (pos == QueBack)
                    return QueBack;
                BufDec(ref pos);
                var count = (pos >= QueBack ? pos - QueBack : pos) + 1;
                if (FindLfBackwards(pos, count, out pos))
                    --lines;
            } while (lines > 0);
            BufInc(ref pos);
        }
        return pos;
    }

    // Pre: count >= 1. Post: 'last' is the last checked position.
    private bool FindLfBackwards(int pos, int count, out int last)
    {
        ++pos;
        do
        {
            if (Buffer[--pos] == '\n')
            {
                last = pos;
                return true;
            }
        } while (--count > 0);
        last = pos;
        return false;
    }

    /// <summary>Whether the buffer is empty.</summary>
    public bool QueEmpty() => QueBack == QueFront;
}
