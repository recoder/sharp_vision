using System;
using System.Diagnostics;

using SharpVision.Drawing;
using SharpVision.Objects;

namespace SharpVision.Platform;

/// <summary>
/// The screen contents (also exposed as <see cref="TScreen.ScreenBuffer"/>), with per-row
/// damage tracking, so that flushing only writes the cells that changed since the last
/// flush. Flushes are limited to a maximum frame rate.
/// </summary>
// Ported from include/tvision/internal/dispbuff.h and source/platform/dispbuff.cpp.
internal sealed class DisplayBuffer
{
    private const int DefaultFps = 60;

    private struct Range
    {
        public int Begin;
        public int End;
    }

    private TScreenCell[] _buffer = [];
    private TScreenCell[] _flushBuffer = [];
    private Range[] _rowDamage = [];
    private readonly bool _wideOverlapping = !OperatingSystem.IsWindows();
    private bool _screenTouched = true;
    private bool _caretOrCursorChanged;
    private TPoint _caretPosition = new(-1, -1);
    private int _newCaretSize;
    private TPoint _cursorPosition = new(-1, -1);
    private bool _cursorVisible;
    private TColorAttr _attrUnderCursor;
    private readonly int _maxFps;
    private readonly long _flushDelayTicks;
    private long _lastFlush;
    private long _pendingFlush;

    public DisplayBuffer(int maxFps)
    {
        _maxFps = maxFps < 0 ? DefaultFps : maxFps;
        if (_maxFps > 0)
            _flushDelayTicks = Stopwatch.Frequency / _maxFps;
    }

    /// <summary>The screen size.</summary>
    public TPoint Size;

    /// <summary>The caret size last sent to the display, or -1.</summary>
    public int CaretSize = -1;

    /// <summary>The cells, written to directly by <c>TView</c>'s write functions.</summary>
    public TScreenCell[] Buffer => _buffer;

    private bool InBounds(int x, int y) => 0 <= x && x < Size.X && 0 <= y && y < Size.Y;

    public TScreenCell[] ReloadScreenInfo(DisplayAdapter display)
    {
        Size = display.ReloadScreenInfo();
        CaretSize = -1;
        ResizeBuffer();
        return _buffer;
    }

    private void ResizeBuffer()
    {
        _buffer = new TScreenCell[Size.X * Size.Y];
        _flushBuffer = new TScreenCell[Size.X * Size.Y];
        _rowDamage = new Range[Size.Y];
        _rowDamage.AsSpan().Fill(new Range { Begin = int.MaxValue, End = int.MinValue });
    }

    public void ClearScreen(DisplayAdapter display)
    {
        display.ClearScreen();
        display.Flush();
        ResizeBuffer();
    }

    public void RedrawScreen(DisplayAdapter display)
    {
        _screenTouched = true;
        _lastFlush = 0;
        Array.Clear(_flushBuffer);
        for (var i = 0; i < _rowDamage.Length; ++i)
            _rowDamage[i] = new Range { Begin = 0, End = Size.X - 1 };
        FlushScreen(display);
    }

    public void SetCaretSize(int size) => _newCaretSize = size;

    public void SetCaretPosition(int x, int y)
    {
        _caretPosition = new TPoint(x, y);
        _caretOrCursorChanged = true;
    }

    public void ScreenWrite(int x, int y, ReadOnlySpan<TScreenCell> buf)
    {
        var len = buf.Length;
        if (InBounds(x, y) && len > 0)
        {
            len = Math.Min(len, Size.X - x);
            var dst = _buffer.AsSpan(y * Size.X + x, len);
            // TScreen.ScreenBuffer is this buffer, so the data is usually already there.
            if (!buf.Overlaps(dst, out var offset) || offset != 0)
                buf[..len].CopyTo(dst);
            SetDirty(x, y, len);
            _screenTouched = true;
        }
    }

    private void SetDirty(int x, int y, int len)
    {
        var dam = _rowDamage[y];
        if (x < dam.Begin)
            dam.Begin = x;
        if (x + len - 1 > dam.End)
            dam.End = x + len - 1;
        _rowDamage[y] = dam;
    }

    // Avoids flushing faster than the maximum frame rate.
    private bool TimeToFlush()
    {
        if (_maxFps > 0)
        {
            var now = Stopwatch.GetTimestamp();
            var flushTime = _lastFlush + _flushDelayTicks;
            if (flushTime <= now)
            {
                _lastFlush = now;
                _pendingFlush = 0;
            }
            else
            {
                _pendingFlush = flushTime;
                return false;
            }
        }
        return true;
    }

    /// <summary>Milliseconds until a deferred flush is due, or -1 if none is pending.</summary>
    public int TimeUntilPendingFlushMs()
    {
        if (_pendingFlush == 0)
            return -1;
        var now = Stopwatch.GetTimestamp();
        if (_pendingFlush < now)
            return 0;
        return (int)((_pendingFlush - now) * 1000 / Stopwatch.Frequency);
    }

    /// <summary>Moves the software mouse cursor (drawn by inverting a cell's colors).</summary>
    public void SetCursorPosition(int x, int y)
    {
        var pos = new TPoint(x, y);
        if (_cursorVisible && _cursorPosition != pos)
            _caretOrCursorChanged = true;
        _cursorPosition = pos;
    }

    /// <summary>Shows or hides the software mouse cursor.</summary>
    public void SetCursorVisibility(bool visible)
    {
        if (_cursorVisible != visible)
            _caretOrCursorChanged = true;
        _cursorVisible = visible;
    }

    private static TColorAttr NegateAttribute(TColorAttr attr) => attr.ToBIOS() ^ 0x77;

    private int CursorCellIndex(ref int x, int y)
    {
        var i = y * Size.X + x;
        if (_buffer[i].Character.IsWideCharTrail && x > 0 && _buffer[i - 1].Character.IsWide)
        {
            --i;
            --x;
        }
        return i;
    }

    private void DrawCursor()
    {
        if (_cursorVisible)
        {
            int x = _cursorPosition.X, y = _cursorPosition.Y;
            if (InBounds(x, y))
            {
                var i = CursorCellIndex(ref x, y);
                _attrUnderCursor = _buffer[i].Attribute;
                _buffer[i].Attribute = NegateAttribute(_buffer[i].Attribute);
                SetDirty(x, y, 1);
            }
        }
    }

    private void UndrawCursor()
    {
        if (_cursorVisible)
        {
            int x = _cursorPosition.X, y = _cursorPosition.Y;
            if (InBounds(x, y))
            {
                var i = CursorCellIndex(ref x, y);
                _buffer[i].Attribute = _attrUnderCursor;
                SetDirty(x, y, 1);
            }
        }
    }

    private bool NeedsFlush() => _screenTouched || _caretOrCursorChanged || CaretSize != _newCaretSize;

    /// <summary>Writes the changed cells and the caret to <paramref name="display"/>, if due.</summary>
    public void FlushScreen(DisplayAdapter display)
    {
        if (NeedsFlush() && TimeToFlush())
        {
            DrawCursor();
            new FlushScreenAlgorithm(this, display).Run();
            if (_caretPosition.X != -1)
                display.SetCaretPosition(_caretPosition);
            UndrawCursor();
            if (CaretSize != _newCaretSize)
                display.SetCaretSize(_newCaretSize);
            display.Flush();
            _screenTouched = false;
            _caretOrCursorChanged = false;
            CaretSize = _newCaretSize;
        }
    }

    // Writes the damaged parts of each row, taking care that double-width characters
    // neither spill into the next line nor leave stale halves or attributes behind.
    private struct FlushScreenAlgorithm(DisplayBuffer disp, DisplayAdapter display)
    {
        private TPoint _size;
        private int _x, _y, _rowOffs, _cell;
        private Range _damage;

        private readonly ref TScreenCell Cell => ref disp._buffer[_cell];

        private readonly bool IsTrail(int i) => disp._buffer[i].Character.IsWideCharTrail;

        private readonly bool IsWide(int i) => disp._buffer[i].Character.IsWide;

        private void GetCell()
        {
            _cell = _rowOffs + _x;
            EnsureCharacterIsPrintable(ref Cell);
        }

        private readonly bool CellDirty() => Cell != disp._flushBuffer[_cell];

        private readonly void CommitDirty() => disp._flushBuffer[_cell] = Cell;

        public void Run()
        {
            _size = disp.Size;
            _rowOffs = 0;
            for (_y = 0; _y < _size.Y; ++_y, _rowOffs += _size.X)
            {
                _damage = disp._rowDamage[_y];
                if (_damage.Begin <= _damage.End)
                {
                    _x = _damage.Begin;
                    // Check for a wide character before the first draw position
                    // if overlapping is not allowed.
                    var wideSpillBefore = !disp._wideOverlapping && 0 < _x && IsWide(_rowOffs + _x - 1);
                    for (; _x <= _damage.End; ++_x)
                    {
                        GetCell();
                        if (CellDirty() || IsTrail(_cell))
                        {
                            if (wideSpillBefore)
                            {
                                --_x;
                                GetCell();
                                wideSpillBefore = false;
                            }
                            CommitDirty();
                            ProcessCell();
                        }
                        else
                            wideSpillBefore = !disp._wideOverlapping && IsWide(_cell);
                    }
                    if (_x < _size.X)
                    {
                        GetCell();
                        if (IsTrail(_cell))
                        {
                            // The beginning of a wide character has been overwritten
                            // and attribute spill may happen in the remaining trails.
                            HandleTrail();
                        }
                    }
                }
                disp._rowDamage[_y] = new Range { Begin = int.MaxValue, End = int.MinValue };
            }
        }

        private void ProcessCell()
        {
            if (IsWide(_cell))
                HandleWideCharSpill();
            else if (IsTrail(_cell))
                HandleTrail();
            else
                WriteCell();
        }

        private readonly void WriteCell() =>
            display.WriteCell(new TPoint(_x, _y), Cell.Character.GetText(), Cell.Attribute, Cell.Character.IsWide);

        private readonly void WriteSpace() =>
            display.WriteCell(new TPoint(_x, _y), " ", Cell.Attribute, false);

        private static void EnsureCharacterIsPrintable(ref TScreenCell cell)
        {
            if (cell.Character.IsWideCharTrail || !cell.Character.IsSingleChar)
                return;
            var c = cell.Character.SingleChar;
            if (c == '\0')
                cell.Character.InitWithChar(' ');
            else if (c < ' ' || c == '\x7F')
                cell.Character.InitWithText(CpTranslator.ToUnicode((byte)c)); // Translate from code page as fallback.
        }

        private void HandleWideCharSpill()
        {
            var width = Cell.Character.IsWide ? 1 : 0;
            var attr = Cell.Attribute;
            if (_x + width < _size.X)
                WriteCell();
            else
            {
                // Replace with spaces if it would otherwise be printed on the next line.
                WriteSpace();
                while (--width != 0 && ++_x < _size.X)
                {
                    GetCell();
                    if (!IsTrail(_cell))
                    {
                        --_x;
                        return;
                    }
                    WriteSpace();
                }
                return;
            }
            // Ensure character does not spill on next cells.
            var wbegin = _x;
            while (width-- != 0 && ++_x < _size.X)
            {
                GetCell();
                CommitDirty();
                if (!IsTrail(_cell))
                {
                    if (disp._wideOverlapping)
                        WriteCell(); // Write over the wide character.
                    else
                    {
                        // Replace the whole wide character with spaces.
                        var wend = _x;
                        _x = wbegin;
                        do
                        {
                            GetCell();
                            WriteSpace();
                        } while (++_x < wend);
                        GetCell();
                        WriteCell();
                    }
                    return;
                }
            }
            // If the next character has a different attribute, print it anyway
            // to avoid attribute spill.
            if (_x + 1 < _size.X)
            {
                ++_x;
                GetCell();
                if (attr != Cell.Attribute)
                {
                    CommitDirty();
                    ProcessCell();
                }
                else
                    --_x;
            }
        }

        private void HandleTrail()
        {
            var attr = Cell.Attribute;
            if (_x > 0)
            {
                --_x;
                GetCell();
                // Check the character behind the placeholder.
                if (Cell.Character.IsWide)
                {
                    HandleWideCharSpill();
                    return;
                }
                ++_x;
                GetCell();
            }
            // Print successive placeholders as spaces.
            do
            {
                CommitDirty();
                WriteSpace();
                ++_x;
                if (_x < _size.X)
                    GetCell();
                else
                    return;
            } while (IsTrail(_cell));
            // We now got a normal character.
            if (_x > _damage.End && attr != Cell.Attribute)
                ProcessCell(); // Redraw a character that would otherwise not be printed, to prevent attribute spill.
            else
                --_x; // Decrease for next iteration.
        }
    }
}
