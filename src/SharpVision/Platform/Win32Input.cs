using SharpVision.Drawing;
using SharpVision.Events;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Platform.Interop.Kernel32;
using static SharpVision.Views.Commands;

namespace SharpVision.Platform;

/// <summary>Reads keyboard, mouse and resize events from the Windows console.</summary>
/// Ported from source/platform/win32con.cpp (Win32Input, getWin32Key, getWin32Mouse) and
/// source/tvision/hardwrvr.cpp (THardwareInfo::NormalCvt, ShiftCvt, CtrlCvt, AltCvt).
internal sealed class Win32Input(ConsoleCtl con) : EventSource(con.In)
{
    private const ushort ControlKeyMask =
        kbShift | kbCtrlShift | kbAltShift | kbScrollState | kbNumState | kbCapsState | kbEnhanced;

    // Convert NT virtual scan codes (index) to PC BIOS key codes, by modifier.
    private static readonly ushort[] _normalCvt =
    [
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0x8500, 0x8600,
    ];

    private static readonly ushort[] _shiftCvt =
    [
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x0F00,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x5400, 0x5500, 0x5600, 0x5700, 0x5800,
        0x5900, 0x5A00, 0x5B00, 0x5C00, 0x5D00, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0x0500, 0x0700, 0, 0, 0, 0x8700, 0x8800,
    ];

    private static readonly ushort[] _ctrlCvt =
    [
        0, 0, 0x0231, 0x0332, 0x0433, 0x0534, 0x0635, 0x0736,
        0x0837, 0x0938, 0x0A39, 0x0B30, 0, 0, 0, 0x9400,
        0x0011, 0x0017, 0x0005, 0x0012, 0x0014, 0x0019, 0x0015, 0x0009,
        0x000F, 0x0010, 0, 0, 0, 0, 0x0001, 0x0013,
        0x0004, 0x0006, 0x0007, 0x0008, 0x000A, 0x000B, 0x000C, 0,
        0, 0, 0, 0, 0x001A, 0x0018, 0x0003, 0x0016,
        0x0002, 0x000E, 0x000D, 0, 0, 0x352F, 0, 0x372A,
        0, 0, 0, 0x5E00, 0x5F00, 0x6000, 0x6100, 0x6200,
        0x6300, 0x6400, 0x6500, 0x6600, 0x6700, 0, 0, 0x7700,
        0x8D00, 0x8400, 0x4A2D, 0x7300, 0, 0x7400, 0x4E2B, 0x7500,
        0x9100, 0x7600, 0x0400, 0x0600, 0, 0, 0, 0x8900,
        0x8A00,
    ];

    private static readonly ushort[] _altCvt =
    [
        0, 0x0100, 0x7800, 0x7900, 0x7A00, 0x7B00, 0x7C00, 0x7D00,
        0x7E00, 0x7F00, 0x8000, 0x8100, 0x8200, 0x8300, 0x0E00, 0xA500,
        0x1000, 0x1100, 0x1200, 0x1300, 0x1400, 0x1500, 0x1600, 0x1700,
        0x1800, 0x1900, 0, 0, 0xA600, 0, 0x1E00, 0x1F00,
        0x2000, 0x2100, 0x2200, 0x2300, 0x2400, 0x2500, 0x2600, 0,
        0, 0, 0, 0, 0x2C00, 0x2D00, 0x2E00, 0x2F00,
        0x3000, 0x3100, 0x3200, 0, 0, 0, 0, 0,
        0, 0x0200, 0, 0x6800, 0x6900, 0x6A00, 0x6B00, 0x6C00,
        0x6D00, 0x6E00, 0x6F00, 0x7000, 0x7100, 0, 0, 0x9700,
        0x9800, 0x9900, 0x8200, 0x9B00, 0, 0x9D00, 0, 0x9F00,
        0xA000, 0xA100, 0xA200, 0xA300, 0, 0, 0, 0x8B00,
        0x8C00,
    ];

    private readonly InputState _state = new();

    public override bool GetEvent(ref TEvent ev)
    {
        // ReadConsoleInput can block, so check the number of available events first. Many
        // records are discarded (e.g. key releases), but don't return false while some remain.
        while (GetNumberOfConsoleInputEvents(Handle, out var events) != 0 && events != 0)
        {
            while (events-- != 0)
            {
                if (ReadConsoleInputW(Handle, out var ir, 1, out var read) == 0 || read == 0)
                    return false;
                if (GetEvent(ir, ref ev))
                    return true;
            }
        }
        return false;
    }

    private bool GetEvent(in INPUT_RECORD ir, ref TEvent ev)
    {
        switch (ir.EventType)
        {
            case KEY_EVENT:
                // Key down, or a pasted surrogate character (reported on Alt release).
                if (ir.KeyEvent.bKeyDown != 0 ||
                    (ir.KeyEvent.wVirtualKeyCode == VK_MENU && ir.KeyEvent.UnicodeChar != 0))
                    return GetWin32Key(ir.KeyEvent, ref ev, _state);
                break;
            case MOUSE_EVENT:
                GetWin32Mouse(ir.MouseEvent, ref ev, _state);
                return true;
            case WINDOW_BUFFER_SIZE_EVENT:
                ev.What = evCommand;
                ev.Message.Command = cmScreenChanged;
                ev.Message.InfoPtr = null;
                return true;
        }
        return false;
    }

    // Returns true unless the event holds a high surrogate, in which case the next one is needed.
    private static bool GetWin32KeyText(in KEY_EVENT_RECORD keyEvent, ref TEvent ev, InputState state)
    {
        var ch = (char)keyEvent.UnicodeChar;
        ev.KeyDown.Text = null;

        // Do not treat non-printable characters as text.
        if (' ' <= ch && ch != 0x7F)
        {
            if (char.IsHighSurrogate(ch))
            {
                state.Surrogate = ch;
                return false;
            }
            string text;
            if (state.Surrogate != 0 && char.IsLowSurrogate(ch))
                text = new string([state.Surrogate, ch]);
            else
                text = char.IsSurrogate(ch) ? "�" : ch.ToString(); // As WideCharToMultiByte does.
            state.Surrogate = '\0';
            ev.KeyDown.Text = text;
        }
        return true;
    }

    /// <summary>Converts a console key record to a key down event. Returns false if it produces no key.</summary>
    internal static bool GetWin32Key(in KEY_EVENT_RECORD keyEvent, ref TEvent ev, InputState state)
    {
        if (!GetWin32KeyText(keyEvent, ref ev, state))
            return false;

        ev.What = evKeyDown;
        ev.KeyDown.CharScan.ScanCode = (byte)keyEvent.wVirtualScanCode;
        ev.KeyDown.CharScan.CharCode = (byte)keyEvent.UnicodeChar; // uChar.AsciiChar.
        ev.KeyDown.ControlKeyState = (ushort)(keyEvent.dwControlKeyState & ControlKeyMask);

        if (!string.IsNullOrEmpty(ev.KeyDown.Text))
        {
            ev.KeyDown.CharScan.CharCode = CpTranslator.FromUnicode(ev.KeyDown.Text);
            if (keyEvent.wVirtualKeyCode == VK_MENU)
                // Set when pasting certain characters, and it confuses applications.
                ev.KeyDown.CharScan.ScanCode = 0;
            if (ev.KeyDown.CharScan.CharCode == 0 || ev.KeyDown.KeyCode <= kbCtrlZ)
                // Not representable in the code page, or it would look like Ctrl+Key.
                ev.KeyDown.KeyCode = kbNoKey;
        }

        var hasText = !string.IsNullOrEmpty(ev.KeyDown.Text);
        if (ev.KeyDown.KeyCode is 0x2A00 or 0x1D00 or 0x3600 or 0x3800 or 0x3A00 or 0x5B00 or 0x5C00)
            // Discard standalone Shift, Ctrl, Alt, Caps Lock, Windows keys.
            ev.KeyDown.KeyCode = kbNoKey;
        else if ((ev.KeyDown.ControlKeyState & kbLeftCtrl) != 0 &&
                 (ev.KeyDown.ControlKeyState & kbRightAlt) != 0 &&
                 !hasText)
            // AltGr adds left Ctrl. Without text, discard it rather than handle it as Ctrl+Alt.
            ev.KeyDown.KeyCode = kbNoKey;
        else if ((ev.KeyDown.ControlKeyState & kbCtrlShift) != 0 &&
                 (ev.KeyDown.ControlKeyState & kbAltShift) != 0 &&
                 hasText)
            // Ctrl+Alt producing text is AltGr: drop the modifiers.
            ev.KeyDown.ControlKeyState &= unchecked((ushort)~(kbCtrlShift | kbAltShift));
        else if (keyEvent.wVirtualScanCode < 89)
        {
            var index = keyEvent.wVirtualScanCode;
            var mods = ev.KeyDown.ControlKeyState;
            ushort keyCode = 0;
            if ((mods & kbAltShift) != 0 && _altCvt[index] != 0)
                keyCode = _altCvt[index];
            else if ((mods & kbCtrlShift) != 0 && _ctrlCvt[index] != 0)
                keyCode = _ctrlCvt[index];
            else if ((mods & kbShift) != 0 && _shiftCvt[index] != 0)
                keyCode = _shiftCvt[index];
            else if ((mods & (kbShift | kbCtrlShift | kbAltShift)) == 0 && _normalCvt[index] != 0)
                keyCode = _normalCvt[index];

            if (keyCode != 0)
            {
                ev.KeyDown.KeyCode = keyCode;
                if (ev.KeyDown.CharScan.CharCode < ' ')
                    ev.KeyDown.Text = null;
                else if (ev.KeyDown.CharScan.CharCode < 0x7F && !hasText)
                    ev.KeyDown.Text = ((char)ev.KeyDown.CharScan.CharCode).ToString();
            }
        }

        return ev.KeyDown.KeyCode != kbNoKey || !string.IsNullOrEmpty(ev.KeyDown.Text);
    }

    /// <summary>Converts a console mouse record to a mouse event.</summary>
    internal static void GetWin32Mouse(in MOUSE_EVENT_RECORD mouseEvent, ref TEvent ev, InputState state)
    {
        ev.What = evMouse;
        ev.Mouse.Where.X = mouseEvent.dwMousePosition.X;
        ev.Mouse.Where.Y = mouseEvent.dwMousePosition.Y;
        ev.Mouse.Buttons = state.Buttons = (byte)mouseEvent.dwButtonState;
        ev.Mouse.EventFlags = (ushort)mouseEvent.dwEventFlags;
        ev.Mouse.ControlKeyState = (ushort)(mouseEvent.dwControlKeyState & ControlKeyMask);

        // The rotation sense is the sign of dwButtonState's high word.
        var positive = (mouseEvent.dwButtonState & 0x80000000) == 0;
        if ((mouseEvent.dwEventFlags & MOUSE_WHEELED) != 0)
            ev.Mouse.Wheel = positive ? mwUp : mwDown;
        else if ((mouseEvent.dwEventFlags & MOUSE_HWHEELED) != 0)
            ev.Mouse.Wheel = positive ? mwRight : mwLeft;
        else
            ev.Mouse.Wheel = 0;
    }
}
