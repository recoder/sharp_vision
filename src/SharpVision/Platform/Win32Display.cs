// Ported from source/platform/win32con.cpp (Win32Display).

using System;
using System.Collections.Generic;

using SharpVision.Drawing;
using SharpVision.Objects;

using static SharpVision.Platform.Interop.Kernel32;

namespace SharpVision.Platform;

/// <summary>
/// Draws to the Windows console: with VT sequences through <see cref="AnsiDisplay"/> when
/// supported, otherwise (legacy console) with the console API.
/// </summary>
internal sealed class Win32Display : DisplayAdapter
{
    private readonly ConsoleCtl _con;
    private readonly AnsiDisplay? _ansi;
    private readonly List<char> _buf = new();
    private TPoint _size;
    private TPoint _caretPos = new(-1, -1);
    private byte _lastAttr;

    public Win32Display(ConsoleCtl con, bool isLegacyConsole)
    {
        _con = con;
        // UPSTREAM: the writer's capabilities are computed while it is still null, so they
        // come from GetColorCount() == 16 (Indexed16, unless COLORTERM says truecolor), even
        // though the display then reports true color.
        if (!isLegacyConsole)
            _ansi = new AnsiDisplay(con, GetColorCount());
    }

    public override TPoint ReloadScreenInfo()
    {
        var lastSize = _size;
        _size = _con.GetSize();

        if (lastSize != _size)
        {
            GetConsoleScreenBufferInfo(_con.Out, out var sbInfo);
            // Move the cursor to (0, 0) meanwhile to avoid a console crash
            // (https://github.com/microsoft/terminal/issues/7511).
            var curPos = sbInfo.dwCursorPosition;
            SetConsoleCursorPosition(_con.Out, new COORD(0, 0));
            // Match the buffer size to the viewport so that no scroll bars are shown. Only on
            // changes, or Wine keeps sending WINDOW_BUFFER_SIZE_EVENTs.
            SetConsoleScreenBufferSize(_con.Out, new COORD((short)_size.X, (short)_size.Y));
            SetConsoleCursorPosition(_con.Out, curPos);
        }

        // shortcut: upstream measures character widths through the console (WinWidth) and
        // resets them on font changes; the managed wcwidth approximation is used instead.

        if (_ansi is not null)
            _ansi.Reset();
        else
        {
            _caretPos = new TPoint(-1, -1);
            _lastAttr = 0;
        }
        return _size;
    }

    // Assume true color outside the legacy console
    // (https://devblogs.microsoft.com/commandline/24-bit-color-in-the-windows-console/).
    public override int GetColorCount() => _ansi is not null ? 256 * 256 * 256 : 16;

    public override TPoint GetFontSize() => _con.GetFontSize();

    public override void SetCaretSize(int size)
    {
        var crInfo = size != 0
            ? new CONSOLE_CURSOR_INFO { bVisible = 1, dwSize = (uint)size }
            : new CONSOLE_CURSOR_INFO { bVisible = 0, dwSize = 1 };
        SetConsoleCursorInfo(_con.Out, crInfo);
    }

    public override void ClearScreen()
    {
        if (_ansi is not null)
        {
            _ansi.ClearScreen();
            return;
        }
        var length = (uint)(_size.X * _size.Y);
        const byte attr = 0x07;
        FillConsoleOutputAttribute(_con.Out, attr, length, new COORD(0, 0), out _);
        FillConsoleOutputCharacterW(_con.Out, ' ', length, new COORD(0, 0), out _);
        _lastAttr = attr;
    }

    public override void WriteCell(TPoint pos, ReadOnlySpan<char> text, TColorAttr attr, bool doubleWidth)
    {
        if (_ansi is not null)
        {
            _ansi.WriteCell(pos, text, attr, doubleWidth);
            return;
        }
        if (pos != _caretPos)
        {
            Flush();
            SetConsoleCursorPosition(_con.Out, new COORD((short)pos.X, (short)pos.Y));
        }
        var biosAttr = (byte)attr;
        if (biosAttr != _lastAttr)
        {
            Flush();
            SetConsoleTextAttribute(_con.Out, biosAttr);
        }
        foreach (var c in text)
            _buf.Add(c);
        _caretPos = new TPoint(pos.X + 1 + (doubleWidth ? 1 : 0), pos.Y);
        _lastAttr = biosAttr;
    }

    public override void SetCaretPosition(TPoint pos)
    {
        if (_ansi is not null)
        {
            _ansi.SetCaretPosition(pos);
            return;
        }
        Flush();
        SetConsoleCursorPosition(_con.Out, new COORD((short)pos.X, (short)pos.Y));
        _caretPos = pos;
    }

    public override void Flush()
    {
        if (_ansi is not null)
        {
            _ansi.Flush();
            return;
        }
        if (_buf.Count == 0)
            return;
        // Legacy console: WriteConsoleW mishandles characters that map to control characters
        // in the OEM code page (e.g. '○' is 0x09, a tab), so use WriteConsoleOutputW. Every
        // UTF-16 code unit takes one cell.
        var cells = new CHAR_INFO[_buf.Count];
        for (var i = 0; i < cells.Length; ++i)
            cells[i] = new CHAR_INFO { UnicodeChar = _buf[i], Attributes = _lastAttr };
        var to = new SMALL_RECT
        {
            Left = (short)(_caretPos.X - cells.Length),
            Top = (short)_caretPos.Y,
            Right = (short)(_caretPos.X - 1),
            Bottom = (short)_caretPos.Y,
        };
        WriteConsoleOutputW(_con.Out, ref cells[0], new COORD((short)cells.Length, 1), new COORD(0, 0), ref to);
        _buf.Clear();
    }
}
