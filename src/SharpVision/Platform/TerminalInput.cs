// Ported from source/platform/ncursinp.cpp (NcursesInput), without ncurses: bytes are read
// from the terminal directly and escape sequences are parsed by TermIO.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Platform.Interop;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Platform;

/// <summary>Reads keyboard and mouse events from the terminal.</summary>
internal sealed class TerminalInput : EventSource, IInputGetter
{
    // How long to wait for the rest of a multibyte character or escape sequence.
    private const int ReadTimeoutMs = 10;

    private static readonly KeyDownEvent[] _fromNonPrintableAscii =
    [
        new() { KeyCode = '@', ControlKeyState = kbLeftCtrl, Text = "@" }, // ^@, Null
        Ctrl(kbCtrlA), Ctrl(kbCtrlB), Ctrl(kbCtrlC), Ctrl(kbCtrlD), Ctrl(kbCtrlE), Ctrl(kbCtrlF), Ctrl(kbCtrlG),
        new() { KeyCode = kbBack }, // ^H, Backspace
        new() { KeyCode = kbTab }, // ^I, Tab
        new() { KeyCode = kbEnter }, // ^J, Line Feed
        Ctrl(kbCtrlK), Ctrl(kbCtrlL),
        new() { KeyCode = kbEnter }, // ^M, Carriage Return
        Ctrl(kbCtrlN), Ctrl(kbCtrlO), Ctrl(kbCtrlP), Ctrl(kbCtrlQ), Ctrl(kbCtrlR), Ctrl(kbCtrlS), Ctrl(kbCtrlT),
        Ctrl(kbCtrlU), Ctrl(kbCtrlV), Ctrl(kbCtrlW), Ctrl(kbCtrlX), Ctrl(kbCtrlY), Ctrl(kbCtrlZ),
        new() { KeyCode = kbEsc }, // ^[, Escape
        new() { KeyCode = '\\', ControlKeyState = kbLeftCtrl, Text = "\\" }, // ^\, File Separator
        new() { KeyCode = ']', ControlKeyState = kbLeftCtrl, Text = "]" }, // ^], Group Separator
        new() { KeyCode = '^', ControlKeyState = kbLeftCtrl, Text = "^" }, // ^^, Record Separator
        new() { KeyCode = '_', ControlKeyState = kbLeftCtrl, Text = "_" }, // ^_, Unit Separator
    ];

    private readonly InputState _state;
    private readonly Stack<int> _ungotten = new();
    private readonly byte[] _readBuf = new byte[4096];
    private int _readPos;
    private int _readLen;

    public TerminalInput(ConsoleCtl con, InputState state)
        : base(con.In)
    {
        _state = state;
    }

    private static KeyDownEvent Ctrl(ushort keyCode) => new() { KeyCode = keyCode, ControlKeyState = kbLeftCtrl };

    public override bool HasPendingEvents() => _ungotten.Count > 0 || _readPos < _readLen;

    /// <summary>Returns the next byte, waiting up to <see cref="ReadTimeoutMs"/>, or -1.</summary>
    public int Get() => Get(ReadTimeoutMs);

    public void Unget(int k) => _ungotten.Push(k);

    private int Get(int timeoutMs)
    {
        if (_ungotten.Count > 0)
            return _ungotten.Pop();
        if (_readPos < _readLen)
            return _readBuf[_readPos++];
        var pfd = new LibC.PollFd { Fd = (int)Handle, Events = LibC.POLLIN };
        if (LibC.poll(ref pfd, 1, timeoutMs) <= 0 || (pfd.REvents & LibC.POLLIN) == 0)
            return -1;
        var n = LibC.read((int)Handle, ref MemoryMarshal.GetReference(_readBuf.AsSpan()), (nuint)_readBuf.Length);
        if (n <= 0)
            return -1;
        _readPos = 0;
        _readLen = (int)n;
        return _readBuf[_readPos++];
    }

    public override bool GetEvent(ref TEvent ev)
    {
        var buf = new GetChBuf(this);
        switch (TermIO.ParseEvent(buf, ref ev, _state))
        {
            case ParseResult.Rejected:
                buf.Reject();
                break;
            case ParseResult.Accepted:
                return true;
            case ParseResult.Ignored:
                return false;
        }

        var k = Get();
        if (k == -1)
            return false;

        ev.What = evKeyDown;
        ev.KeyDown = default;
        var alt = false;

        // Alt+Key combinations begin with ESC. To tell the difference, check whether
        // another byte is already available.
        if (k == 0x1B)
        {
            var next = Get(0);
            if (next != -1)
            {
                k = next;
                alt = true;
            }
        }

        if (k < 32)
            ev.KeyDown = _fromNonPrintableAscii[k];
        else if (k == 127)
            ev.KeyDown = new KeyDownEvent { KeyCode = kbBack }; // ^?, Delete

        // If it hasn't been transformed by the previous tables, it is a printable character.
        if (ev.KeyDown.KeyCode == kbNoKey)
            ParsePrintableChar(ref ev, k);

        if (alt)
        {
            ev.KeyDown.ControlKeyState |= kbLeftAlt;
            TermIO.NormalizeKey(ref ev.KeyDown);
        }
        if (_state.BracketedPaste)
            ev.KeyDown.ControlKeyState |= kbPaste;

        return ev.KeyDown.KeyCode != kbNoKey || ev.KeyDown.Text is not null;
    }

    private void ParsePrintableChar(ref TEvent ev, int first)
    {
        // Unicode characters are sent byte by byte. Predict the number of bytes from the
        // first one, then read as many.
        Span<byte> bytes = stackalloc byte[4];
        bytes[0] = (byte)first;
        var count = 1 + Utf8BytesLeft((byte)first);
        for (var i = 1; i < count; ++i)
        {
            var b = Get();
            if (b == -1)
            {
                count = i;
                break;
            }
            bytes[i] = (byte)b;
        }
        ev.KeyDown.Text = Encoding.UTF8.GetString(bytes[..count]);
        // If we are lucky enough, the character is representable in the active code page.
        ev.KeyDown.CharScan.CharCode = TText.ToCodePage(ev.KeyDown.Text);
        // Prevent text from triggering Ctrl+Key shortcuts.
        if (ev.KeyDown.KeyCode <= kbCtrlZ)
            ev.KeyDown.KeyCode = kbNoKey;
    }

    private static int Utf8BytesLeft(byte first) =>
        (first & 0xE0) == 0xC0 ? 1 :
        (first & 0xF0) == 0xE0 ? 2 :
        (first & 0xF8) == 0xF0 ? 3 : 0;
}
