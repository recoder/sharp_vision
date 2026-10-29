// Ported from source/tvision/tvwrite.cpp and include/tvision/views.h (TView write functions).

using System;

using SharpVision.Drawing;
using SharpVision.Platform;

using static SharpVision.Drawing.StyleFlags;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Views;

public partial class TView
{
    /// <summary>
    /// Writes <paramref name="h"/> rows of <paramref name="w"/> cells from <paramref name="b"/>
    /// (row after row) at (<paramref name="x"/>, <paramref name="y"/>).
    /// </summary>
    public void WriteBuf(int x, int y, int w, int h, ReadOnlySpan<TScreenCell> b)
    {
        // UPSTREAM: upstream reads past the end of 'b' if it holds fewer than w*h cells;
        // here the rows stop where 'b' ends.
        if ((short)w <= 0)
            return;
        while (h-- > 0)
        {
            WriteView(x, y++, w, b);
            if (b.Length <= (short)w)
                break;
            b = b[(short)w..];
        }
    }

    /// <summary>Writes <paramref name="h"/> rows of <paramref name="w"/> cells from <paramref name="b"/> at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public void WriteBuf(int x, int y, int w, int h, TDrawBuffer b) =>
        WriteBuf(x, y, Math.Min((short)w, (short)(b.Capacity - x)), h, b.Data);

    /// <summary>Writes the character <paramref name="c"/> <paramref name="count"/> times at (<paramref name="x"/>, <paramref name="y"/>) in palette color <paramref name="color"/>.</summary>
    public void WriteChar(int x, int y, char c, byte color, int count)
    {
        count = (short)count;
        if (count > Size.X)
            count = Size.X;
        if (count > 0)
        {
            var cells = new TScreenCell[count];
            TText.DrawChar(cells, c, MapColor(color));
            WriteView(x, y, count, cells);
        }
    }

    /// <summary>Writes the same row of <paramref name="w"/> cells from <paramref name="b"/> to <paramref name="h"/> rows starting at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public void WriteLine(int x, int y, int w, int h, ReadOnlySpan<TScreenCell> b)
    {
        while (h-- > 0)
            WriteView(x, y++, w, b);
    }

    /// <summary>Writes the same row of <paramref name="w"/> cells from <paramref name="b"/> to <paramref name="h"/> rows starting at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public void WriteLine(int x, int y, int w, int h, TDrawBuffer b) =>
        WriteLine(x, y, Math.Min((short)w, (short)(b.Capacity - x)), h, b.Data);

    /// <summary>Writes <paramref name="str"/> at (<paramref name="x"/>, <paramref name="y"/>) in palette color <paramref name="color"/>.</summary>
    public void WriteStr(int x, int y, ReadOnlySpan<char> str, byte color)
    {
        var count = TText.Width(str);
        if (count > Size.X)
            count = Size.X;
        if (count > 0)
        {
            var cells = new TScreenCell[count];
            TText.DrawStr(cells, str, MapColor(color));
            WriteView(x, y, count, cells);
        }
    }

    // Writes 'count' cells of 'b' at (x, y), clipping them against the owners and the
    // views in front, and copying them into the buffers of the owners.
    private void WriteView(int x, int y, int count, ReadOnlySpan<TScreenCell> b)
    {
        // UPSTREAM: upstream reads past the end of 'b' if it holds fewer than 'count' cells.
        count = Math.Min((short)count, b.Length);
        new TVWrite(b).L0(this, (short)x, (short)y, (short)count);
    }

    private static TColorAttr ApplyShadow(TColorAttr attr)
    {
        // A dedicated style flag records whether the shadow has already been applied.
        var style = attr.Style;
        if ((style & slWindowShadow) == 0)
        {
            if ((byte)attr.Background.ToBIOS(false) == 0)
                attr = ShadowAttr.Reversed(); // Reverse the shadow attribute on black areas.
            else
                attr = ShadowAttr;
            attr.Style = (ushort)(style | slWindowShadow);
        }
        return attr;
    }

    // A transcription of upstream's register-level algorithm (itself a translation of
    // TVWRITE.ASM). The labels and variable names follow upstream to ease comparison.
    // 'edx' counts the shadows covering the run being written.
    private ref struct TVWrite(ReadOnlySpan<TScreenCell> buffer)
    {
        private readonly ReadOnlySpan<TScreenCell> _buffer = buffer;
        private short _x, _y, _count, _wOffset;
        private TView? _target;
        private int _edx, _esi;

        public void L0(TView dest, short x, short y, short count)
        {
            _x = x;
            _y = y;
            _count = count;
            _wOffset = _x;
            _count += _x;
            _edx = 0;
            if (0 <= _y && _y < dest.Size.Y)
            {
                if (_x < 0)
                    _x = 0;
                if (_count > dest.Size.X)
                    _count = (short)dest.Size.X;
                if (_x < _count)
                    L10(dest);
            }
        }

        private void L10(TView dest)
        {
            var owner = dest.Owner;
            if ((dest.State & sfVisible) != 0 && owner is not null)
            {
                _target = dest;
                _y += (short)dest.Origin.Y;
                _x += (short)dest.Origin.X;
                _count += (short)dest.Origin.X;
                _wOffset += (short)dest.Origin.X;
                if (owner.Clip.A.Y <= _y && _y < owner.Clip.B.Y)
                {
                    if (_x < owner.Clip.A.X)
                        _x = (short)owner.Clip.A.X;
                    if (_count > owner.Clip.B.X)
                        _count = (short)owner.Clip.B.X;
                    if (_x < _count)
                        L20(owner.Last!);
                }
            }
        }

        private void L20(TView dest)
        {
            var next = dest.Next!;
            if (next == _target)
            {
                L40(next);
                return;
            }

            if ((next.State & sfVisible) != 0 && next.Origin.Y <= _y)
            {
                do
                {
                    _esi = next.Origin.Y + next.Size.Y;
                    if (_y < _esi)
                    {
                        _esi = next.Origin.X;
                        if (_x < _esi)
                        {
                            if (_count > _esi)
                                L30(next);
                            else
                                break;
                        }
                        _esi += next.Size.X;
                        if (_x < _esi)
                        {
                            if (_count > _esi)
                                _x = (short)_esi;
                            else
                                return;
                        }
                        if ((next.State & sfShadow) != 0 && next.Origin.Y + ShadowSize.Y <= _y)
                            _esi += ShadowSize.X;
                        else
                            break;
                    }
                    else if ((next.State & sfShadow) != 0 && _y < _esi + ShadowSize.Y)
                    {
                        _esi = next.Origin.X + ShadowSize.X;
                        if (_x < _esi)
                        {
                            if (_count > _esi)
                                L30(next);
                            else
                                break;
                        }
                        _esi += next.Size.X;
                    }
                    else
                        break;
                    if (_x < _esi)
                    {
                        _edx++;
                        if (_count > _esi)
                        {
                            L30(next);
                            _edx--;
                        }
                    }
                } while (false);
            }
            L20(next);
        }

        private void L30(TView dest)
        {
            var target = _target;
            int wOffset = _wOffset, esi = _esi, edx = _edx, count = _count, y = _y;
            _count = (short)_esi;

            L20(dest);

            _y = (short)y;
            _count = (short)count;
            _edx = edx;
            _esi = esi;
            _wOffset = (short)wOffset;
            _target = target;
            _x = (short)esi;
        }

        private void L40(TView dest)
        {
            var owner = dest.Owner!;
            if (owner.Buffer is not null)
                L50(owner);
            if (owner.LockFlag == 0)
                L10(owner);
        }

        private readonly void L50(TGroup owner)
        {
            var length = _count - _x;
            var dst = owner.Buffer.AsSpan(_y * owner.Size.X + _x, length);
            var src = _buffer.Slice(_x - _wOffset, length);
            if (_edx == 0)
                src.CopyTo(dst);
            else
            {
                for (var i = 0; i < length; ++i)
                {
                    var c = src[i];
                    c.Attribute = ApplyShadow(c.Attribute);
                    dst[i] = c;
                }
            }
            if (owner.Buffer == TScreen.ScreenBuffer)
                THardwareInfo.ScreenWrite(_x, _y, dst);
        }
    }
}
