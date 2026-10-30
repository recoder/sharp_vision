using System;
using System.Collections.Generic;
using System.Text;

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Platform;

// Ported from include/tvision/internal/termio.h and source/platform/termio.cpp.

/// <summary>Input state shared between the parser and the input adapter.</summary>
internal sealed class InputState
{
    public byte Buttons;
    public bool BracketedPaste;
    public bool GotDsrResponse;

    // A UTF-16 high surrogate waiting for its low half (Windows console input).
    public char Surrogate;
}

/// <summary>A source of input bytes (-1 when none is available in time) that supports pushing bytes back.</summary>
internal interface IInputGetter
{
    int Get();

    void Unget(int k);
}

/// <summary>Records the bytes read while parsing so they can be pushed back if parsing fails.</summary>
internal sealed class GetChBuf(IInputGetter input)
{
    private const int MaxSize = 31;
    private readonly int[] _keys = new int[MaxSize];
    private int _size;

    public IInputGetter In { get; } = input;

    public int Get(bool keepErr = false)
    {
        if (_size < MaxSize)
        {
            var k = In.Get();
            if (keepErr || k != -1)
                _keys[_size++] = k;
            return k;
        }
        return -1;
    }

    public int Last(int i = 0) => i < _size ? _keys[_size - 1 - i] : -1;

    public void Unget()
    {
        int k;
        if (_size != 0 && (k = _keys[--_size]) != -1)
            In.Unget(k);
    }

    public void Reject()
    {
        while (_size != 0)
            Unget();
    }

    // GetNum, GetInt: INVARIANT: the last non-digit read key (or -1) can be accessed
    // with 'Last()' and can also be ungetted.

    public bool GetNum(out uint result)
    {
        uint num = 0, digits = 0;
        int k;
        while ((k = Get(true)) != -1 && '0' <= k && k <= '9')
        {
            num = 10 * num + (uint)(k - '0');
            ++digits;
        }
        result = num;
        return digits != 0;
    }

    public bool GetInt(out int result)
    {
        int num = 0, digits = 0, sign = 1;
        var k = Get(true);
        if (k == '-')
        {
            sign = -1;
            k = Get(true);
        }
        while (k != -1 && '0' <= k && k <= '9')
        {
            num = 10 * num + (k - '0');
            ++digits;
            k = Get(true);
        }
        result = sign * num;
        return digits != 0;
    }

    public bool ReadStr(string str)
    {
        var origSize = _size;
        var i = 0;
        while (i < str.Length && Get() == str[i])
            ++i;
        if (i == str.Length)
            return true;
        while (origSize < _size)
            Unget();
        return false;
    }
}

/// <summary>
/// The data in a CSI escape sequence. For "\x1B[123::456;789u": length 4, terminator 'u',
/// values 123, (default), 456, 789 and separators ':', ':', ';'.
/// </summary>
internal sealed class CSIData
{
    public const int MaxLength = 6;
    private readonly uint[] _values = new uint[MaxLength];
    private readonly char[] _separators = new char[MaxLength - 1];

    public int Length;
    public char Terminator;

    // Pre: "\x1B[" has just been read.
    public bool ReadFrom(GetChBuf buf)
    {
        for (var i = 0; i < MaxLength; ++i)
        {
            if (!buf.GetNum(out _values[i]))
                _values[i] = uint.MaxValue;
            var k = buf.Last();
            if (k == -1)
                return false; // No more input and CSI is not yet complete.
            if (k is ';' or ':')
            {
                if (i < MaxLength - 1)
                    _separators[i] = (char)k;
            }
            else
            {
                Terminator = (char)k;
                Length = i + 1;
                return true;
            }
        }
        return false; // CSI may be longer than supported.
    }

    public uint GetValue(int i, uint defaultValue = 1) =>
        i < Length && _values[i] != uint.MaxValue ? _values[i] : defaultValue;

    public char GetSeparator(int i) =>
        Length > 0 && i < Length - 1 ? _separators[i] : '\0';
}

internal enum ParseResult
{
    Rejected = 0,
    Accepted,
    Ignored,
}

/// <summary>Parses terminal escape sequences into events.</summary>
/// <remarks>
/// UPSTREAM: the far2l and Conpty win32-input-mode extensions are not ported (they are not
/// requested in <see cref="KeyModsOn"/>), nor is clipboard access through OSC 52.
/// </remarks>
internal static class TermIO
{
    private const uint XTermModDefault = 1;
    private const uint mmAlt = 0x08;
    private const uint mmCtrl = 0x10;

    private static readonly Dictionary<ushort, ushort[]> _moddedKeyCodes = new()
    {
        ['A'] = [0, kbCtrlA, kbAltA],
        ['B'] = [0, kbCtrlB, kbAltB],
        ['C'] = [0, kbCtrlC, kbAltC],
        ['D'] = [0, kbCtrlD, kbAltD],
        ['E'] = [0, kbCtrlE, kbAltE],
        ['F'] = [0, kbCtrlF, kbAltF],
        ['G'] = [0, kbCtrlG, kbAltG],
        ['H'] = [0, kbCtrlH, kbAltH],
        ['I'] = [0, kbCtrlI, kbAltI],
        ['J'] = [0, kbCtrlJ, kbAltJ],
        ['K'] = [0, kbCtrlK, kbAltK],
        ['L'] = [0, kbCtrlL, kbAltL],
        ['M'] = [0, kbCtrlM, kbAltM],
        ['N'] = [0, kbCtrlN, kbAltN],
        ['O'] = [0, kbCtrlO, kbAltO],
        ['P'] = [0, kbCtrlP, kbAltP],
        ['Q'] = [0, kbCtrlQ, kbAltQ],
        ['R'] = [0, kbCtrlR, kbAltR],
        ['S'] = [0, kbCtrlS, kbAltS],
        ['T'] = [0, kbCtrlT, kbAltT],
        ['U'] = [0, kbCtrlU, kbAltU],
        ['V'] = [0, kbCtrlV, kbAltV],
        ['W'] = [0, kbCtrlW, kbAltW],
        ['X'] = [0, kbCtrlX, kbAltX],
        ['Y'] = [0, kbCtrlY, kbAltY],
        ['Z'] = [0, kbCtrlZ, kbAltZ],
        ['1'] = [0, 0, kbAlt1],
        ['2'] = [0, 0, kbAlt2],
        ['3'] = [0, 0, kbAlt3],
        ['4'] = [0, 0, kbAlt4],
        ['5'] = [0, 0, kbAlt5],
        ['6'] = [0, 0, kbAlt6],
        ['7'] = [0, 0, kbAlt7],
        ['8'] = [0, 0, kbAlt8],
        ['9'] = [0, 0, kbAlt9],
        ['0'] = [0, 0, kbAlt0],
        [' '] = [0, 0, kbAltSpace],
        ['-'] = [0, 0, kbAltMinus],
        ['='] = [0, 0, kbAltEqual],
        [kbF1] = [kbShiftF1, kbCtrlF1, kbAltF1],
        [kbF2] = [kbShiftF2, kbCtrlF2, kbAltF2],
        [kbF3] = [kbShiftF3, kbCtrlF3, kbAltF3],
        [kbF4] = [kbShiftF4, kbCtrlF4, kbAltF4],
        [kbF5] = [kbShiftF5, kbCtrlF5, kbAltF5],
        [kbF6] = [kbShiftF6, kbCtrlF6, kbAltF6],
        [kbF7] = [kbShiftF7, kbCtrlF7, kbAltF7],
        [kbF8] = [kbShiftF8, kbCtrlF8, kbAltF8],
        [kbF9] = [kbShiftF9, kbCtrlF9, kbAltF9],
        [kbF10] = [kbShiftF10, kbCtrlF10, kbAltF10],
        [kbF11] = [kbShiftF11, kbCtrlF11, kbAltF11],
        [kbF12] = [kbShiftF12, kbCtrlF12, kbAltF12],
        [kbEsc] = [0, 0, kbAltEsc],
        [kbBack] = [0, kbCtrlBack, kbAltBack],
        [kbTab] = [kbShiftTab, kbCtrlTab, kbAltTab],
        [kbEnter] = [0, kbCtrlEnter, kbAltEnter],
        [kbHome] = [0, kbCtrlHome, kbAltHome],
        [kbUp] = [0, kbCtrlUp, kbAltUp],
        [kbPgUp] = [0, kbCtrlPgUp, kbAltPgUp],
        [kbLeft] = [0, kbCtrlLeft, kbAltLeft],
        [kbRight] = [0, kbCtrlRight, kbAltRight],
        [kbEnd] = [0, kbCtrlEnd, kbAltEnd],
        [kbDown] = [0, kbCtrlDown, kbAltDown],
        [kbPgDn] = [0, kbCtrlPgDn, kbAltPgDn],
        [kbIns] = [kbShiftIns, kbCtrlIns, kbAltIns],
        [kbDel] = [kbShiftDel, kbCtrlDel, kbAltDel],
    };

    private static ushort GetModdedKeyCode(ushort keyCode, ushort tvMods)
    {
        // Modifier precedence: Shift < Ctrl < Alt.
        var largestMod = (tvMods & kbLeftAlt) != 0 ? 2 : (tvMods & kbLeftCtrl) != 0 ? 1 : 0;
        return _moddedKeyCodes.TryGetValue(keyCode, out var codes) ? codes[largestMod] : (ushort)0;
    }

    private static KeyDownEvent KeyWithXTermMods(ushort keyCode, uint mods)
    {
        mods -= XTermModDefault;
        var tvMods = (ushort)(
            ((mods & 1) != 0 ? kbShift : 0) |
            ((mods & 2) != 0 ? kbLeftAlt : 0) |
            ((mods & 4) != 0 ? kbLeftCtrl : 0));
        var keyDown = new KeyDownEvent { KeyCode = keyCode, ControlKeyState = tvMods };
        NormalizeKey(ref keyDown);
        return keyDown;
    }

    private static bool IsAlpha(uint ascii) => ascii >= ' ' && ascii < 127;

    private static bool IsAsciiLetter(uint ch) => ch is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    private static bool IsPrivate(uint codepoint) => codepoint is >= 57344 and <= 63743;

    private static void SetText(ref KeyDownEvent keyDown, uint codepoint)
    {
        if (!Rune.IsValid((int)codepoint))
            return;
        keyDown.Text = new Rune((int)codepoint).ToString();
        var cp = TText.ToCodePage(keyDown.Text);
        keyDown.CharScan.CharCode = cp < ' ' ? (byte)0 : cp;
    }

    private static bool KeyFromCodepoint(uint value, uint mods, out KeyDownEvent keyDown)
    {
        ushort keyCode = value switch
        {
            8 => kbBack,
            9 => kbTab,
            13 => kbEnter,
            27 => kbEsc,
            127 => kbBack,
            // Functional keys as represented in Kitty's keyboard protocol (keypad).
            57399 => '0',
            57400 => '1',
            57401 => '2',
            57402 => '3',
            57403 => '4',
            57404 => '5',
            57405 => '6',
            57406 => '7',
            57407 => '8',
            57408 => '9',
            57409 => '.',
            57410 => '/',
            57411 => '*',
            57412 => '-',
            57413 => '+',
            57414 => kbEnter,
            57415 => '=',
            57416 => ',',
            57417 => kbLeft,
            57418 => kbRight,
            57419 => kbUp,
            57420 => kbDown,
            57421 => kbPgUp,
            57422 => kbPgDn,
            57423 => kbHome,
            57424 => kbEnd,
            57425 => kbIns,
            57426 => kbDel,
            _ => IsAlpha(value) ? (ushort)value : (ushort)0,
        };
        keyDown = KeyWithXTermMods(keyCode, mods);
        if (IsAlpha(keyDown.KeyCode) || (keyDown.KeyCode == 0 && value >= ' ' && !IsPrivate(value)))
            SetText(ref keyDown, keyDown.KeyCode == 0 ? value : keyDown.KeyCode);
        return keyDown.KeyCode != 0 || keyDown.Text is not null;
    }

    private static bool KeyFromLetter(uint letter, uint mod, out KeyDownEvent keyDown)
    {
        ushort keyCode;
        switch (letter)
        {
            case 'A': keyCode = kbUp; break;
            case 'B': keyCode = kbDown; break;
            case 'C': keyCode = kbRight; break;
            case 'D': keyCode = kbLeft; break;
            case 'E': keyCode = kbNoKey; break; // Numpad 5, "KP_Begin".
            case 'F': keyCode = kbEnd; break;
            case 'H': keyCode = kbHome; break;
            case 'P': keyCode = kbF1; break;
            case 'Q': keyCode = kbF2; break;
            case 'R': keyCode = kbF3; break;
            case 'S': keyCode = kbF4; break;
            // UPSTREAM: upstream maps 'Z' to kbTab, relying on ncurses to recognize the
            // unmodified "\x1B[Z" (terminfo kcbt) as Shift+Tab. Without curses, it is mapped here.
            case 'Z': keyCode = kbShiftTab; break;
            // Keypad in XTerm (SS3).
            case 'j': keyCode = '*'; break;
            case 'k': keyCode = '+'; break;
            case 'm': keyCode = '-'; break;
            case 'M': keyCode = kbEnter; break;
            case 'n': keyCode = kbDel; break;
            case 'o': keyCode = '/'; break;
            case 'p': keyCode = kbIns; break;
            case 'q': keyCode = kbEnd; break;
            case 'r': keyCode = kbDown; break;
            case 's': keyCode = kbPgDn; break;
            case 't': keyCode = kbLeft; break;
            case 'u': keyCode = kbNoKey; break; // Numpad 5, "KP_Begin".
            case 'v': keyCode = kbRight; break;
            case 'w': keyCode = kbHome; break;
            case 'x': keyCode = kbUp; break;
            case 'y': keyCode = kbPgUp; break;
            default:
                keyDown = default;
                return false;
        }
        keyDown = KeyWithXTermMods(keyCode, mod);
        if (IsAlpha(keyDown.KeyCode))
            keyDown.Text = ((char)keyDown.KeyCode).ToString();
        return true;
    }

    /// <summary>The sequence that enables mouse reporting.</summary>
    public const string MouseOnSeq =
        "\x1B[?1001s" + // Save old highlight mouse reporting.
        "\x1B[?1000h" + // Enable mouse reporting.
        "\x1B[?1002h" + // Enable mouse drag reporting.
        "\x1B[?1006h";  // Enable SGR extended mouse reporting.

    /// <summary>The sequence that disables mouse reporting.</summary>
    public const string MouseOffSeq =
        "\x1B[?1006l" + // Disable SGR extended mouse reporting.
        "\x1B[?1002l" + // Disable mouse drag reporting.
        "\x1B[?1000l" + // Disable mouse reporting.
        "\x1B[?1001r";  // Restore old highlight mouse reporting.

    /// <summary>Enables the keyboard extensions this parser understands.</summary>
    public static void KeyModsOn(ConsoleCtl con) => con.Write(
        "\x1B[?1036s" + // Save metaSendsEscape (XTerm).
        "\x1B[?1036h" + // Enable metaSendsEscape (XTerm).
        "\x1B[?2004s" + // Save bracketed paste.
        "\x1B[?2004h" + // Enable bracketed paste.
        "\x1B[>4;1m" +  // Enable modifyOtherKeys (XTerm).
        "\x1B[>5u" +    // Disambiguate escape codes (1) + Report alternate keys (4) (Kitty).
                        // Some terminals do not recognize the sequences above and will display
                        // them on screen. Clear the screen to prevent this.
        "\x1B[2J");

    /// <summary>Restores the keyboard modes changed by <see cref="KeyModsOn"/>.</summary>
    public static void KeyModsOff(ConsoleCtl con) => con.Write(
        "\x1B[<u" +     // Restore previous keyboard mode (Kitty).
        "\x1B[>4m" +    // Reset modifyOtherKeys (XTerm).
        "\x1B[?2004l" + // Disable bracketed paste.
        "\x1B[?2004r" + // Restore bracketed paste.
        "\x1B[?1036r"); // Restore metaSendsEscape (XTerm).

    /// <summary>
    /// Replaces the key code with its modified variant (e.g. Ctrl+Left for Left with Ctrl)
    /// and normalizes the modifiers.
    /// </summary>
    public static void NormalizeKey(ref KeyDownEvent keyDown)
    {
        TKey tKey = keyDown;
        var newMods = (ushort)(tKey.Mods & (kbShift | kbLeftCtrl | kbLeftAlt));
        if (newMods != 0)
        {
            var keyCode = GetModdedKeyCode(tKey.Code, newMods);
            if (keyCode != 0)
            {
                keyDown.KeyCode = keyCode;
                if (keyDown.CharScan.CharCode < ' ')
                    keyDown.Text = null;
            }
        }
        // TKey does not distinguish left/right modifiers, so preserve those when available.
        var origMods = keyDown.ControlKeyState;
        keyDown.ControlKeyState = (ushort)(
            ((origMods | newMods) & ~(kbCtrlShift | kbAltShift))
            | (((origMods & kbCtrlShift) != 0 ? origMods : newMods) & kbCtrlShift)
            | (((origMods & kbAltShift) != 0 ? origMods : newMods) & kbAltShift));
    }

    public static ParseResult ParseEvent(GetChBuf buf, ref TEvent ev, InputState state)
    {
        if (buf.Get() == '\x1B')
            return ParseEscapeSeq(buf, ref ev, state);
        return ParseResult.Rejected;
    }

    // Pre: "\x1B" has just been read.
    public static ParseResult ParseEscapeSeq(GetChBuf buf, ref TEvent ev, InputState state)
    {
        var res = ParseResult.Rejected;
        switch (buf.Get())
        {
            case '[':
                switch (buf.Get())
                {
                    case 'M':
                        return ParseX10Mouse(buf, ref ev, state) == ParseResult.Accepted ? ParseResult.Accepted : ParseResult.Ignored;
                    case '<':
                        return ParseSGRMouse(buf, ref ev, state) == ParseResult.Accepted ? ParseResult.Accepted : ParseResult.Ignored;
                    default:
                        {
                            buf.Unget();
                            var csi = new CSIData();
                            if (csi.ReadFrom(buf))
                            {
                                return csi.Terminator switch
                                {
                                    'u' => ParseKittyKey(csi, ref ev),
                                    'R' => ParseCPR(csi, state),
                                    _ => ParseCSIKey(csi, ref ev, state),
                                };
                            }
                            break;
                        }
                }
                break;
            case 'O':
                return ParseSS3Key(buf, ref ev);
            case 'P':
            case ']':
                return ReadUntilBelOrSt(buf) ? ParseResult.Ignored : ParseResult.Rejected; // DCS, OSC.
            case '\x1B':
                res = ParseEscapeSeq(buf, ref ev, state);
                if (res == ParseResult.Accepted && ev.What == evKeyDown)
                {
                    ev.KeyDown.ControlKeyState |= kbLeftAlt;
                    NormalizeKey(ref ev.KeyDown);
                }
                break;
        }
        return res;
    }

    private static ushort MouseMods(uint mod) => (ushort)(
        ((mod & mmAlt) != 0 ? kbLeftAlt : 0) | ((mod & mmCtrl) != 0 ? kbLeftCtrl : 0));

    // Pre: "\x1B[M" has just been read. The complete sequence looks like "\x1B[Mabc", where
    // 'a' is the button number plus 32 and 'b'/'c' the one-based column/row plus 32.
    public static ParseResult ParseX10Mouse(GetChBuf buf, ref TEvent ev, InputState state)
    {
        var butm = (uint)buf.Get();
        var mod = butm & (mmAlt | mmCtrl);
        var but = (butm & ~(mmAlt | mmCtrl)) - 32;
        if (255 - 32 < but)
            return ParseResult.Rejected;
        Span<int> pos = stackalloc int[2];
        for (var n = 0; n < 2; ++n)
        {
            var i = buf.Get();
            if (i < 0 || 255 < i)
                return ParseResult.Rejected;
            // The encoding only supports [0, 222], but some terminals (e.g. urxvt) keep
            // counting, causing an overflow that can be undone.
            if (i > 32)
                i -= 32;
            else
                i += 256 - 32;
            pos[n] = i - 1; // Make it zero-based.
        }

        ev.What = evMouse;
        ev.Mouse = new MouseEventType { Where = new TPoint(pos[0], pos[1]), ControlKeyState = MouseMods(mod) };
        switch (but)
        {
            case 0: // Press.
            case 32: // Drag.
                state.Buttons |= mbLeftButton; break;
            case 1:
            case 33:
                state.Buttons |= mbMiddleButton; break;
            case 2:
            case 34:
                state.Buttons |= mbRightButton; break;
            case 3: state.Buttons = 0; break; // Release.
            case 64: ev.Mouse.Wheel = mwUp; break;
            case 65: ev.Mouse.Wheel = mwDown; break;
            case 66: ev.Mouse.Wheel = mwLeft; break;
            case 67: ev.Mouse.Wheel = mwRight; break;
        }
        ev.Mouse.Buttons = state.Buttons;
        return ParseResult.Accepted;
    }

    // Pre: "\x1B[<" has just been read. The complete sequence looks like "\x1B[<a;b;cM"
    // (press, wheel or drag) or "\x1B[<a;b;cm" (release), where 'a' is the button number
    // and 'b'/'c' the one-based column/row, all in decimal.
    public static ParseResult ParseSGRMouse(GetChBuf buf, ref TEvent ev, InputState state)
    {
        if (!buf.GetNum(out var butm))
            return ParseResult.Rejected;
        var mod = butm & (mmAlt | mmCtrl);
        var but = butm & ~(mmAlt | mmCtrl);
        // IntelliJ may emit negative coordinates.
        if (!buf.GetInt(out var col) || !buf.GetInt(out var row))
            return ParseResult.Rejected;
        row = Math.Max(row, 1) - 1;
        col = Math.Max(col, 1) - 1;
        var type = (uint)buf.Last();
        if (type is not ('M' or 'm'))
            return ParseResult.Rejected;

        ev.What = evMouse;
        ev.Mouse = new MouseEventType { Where = new TPoint(col, row), ControlKeyState = MouseMods(mod) };
        if (type == 'M')
        {
            switch (but)
            {
                case 0:
                case 32:
                    state.Buttons |= mbLeftButton; break;
                case 1:
                case 33:
                    state.Buttons |= mbMiddleButton; break;
                case 2:
                case 34:
                    state.Buttons |= mbRightButton; break;
                case 64: ev.Mouse.Wheel = mwUp; break;
                case 65: ev.Mouse.Wheel = mwDown; break;
                case 66: ev.Mouse.Wheel = mwLeft; break;
                case 67: ev.Mouse.Wheel = mwRight; break;
            }
        }
        else
        {
            switch (but)
            {
                case 0: state.Buttons &= unchecked((byte)~mbLeftButton); break;
                case 1: state.Buttons &= unchecked((byte)~mbMiddleButton); break;
                case 2: state.Buttons &= unchecked((byte)~mbRightButton); break;
            }
        }
        ev.Mouse.Buttons = state.Buttons;
        return ParseResult.Accepted;
    }

    private static KeyDownEvent Key(ushort keyCode, ushort mods = 0) =>
        new() { KeyCode = keyCode, ControlKeyState = mods };

    // https://invisible-island.net/xterm/xterm-function-keys.html
    public static ParseResult ParseCSIKey(CSIData csi, ref TEvent ev, InputState state)
    {
        var terminator = (uint)csi.Terminator;
        if (csi.Length == 1 && terminator == '~')
        {
            switch (csi.GetValue(0))
            {
                case 1: ev.KeyDown = Key(kbHome); break;
                case 2: ev.KeyDown = Key(kbIns); break;
                case 3: ev.KeyDown = Key(kbDel); break;
                case 4: ev.KeyDown = Key(kbEnd); break;
                case 5: ev.KeyDown = Key(kbPgUp); break;
                case 6: ev.KeyDown = Key(kbPgDn); break;
                // UPSTREAM: 7 and 8 (rxvt Home/End) are left to terminfo upstream.
                case 7: ev.KeyDown = Key(kbHome); break;
                case 8: ev.KeyDown = Key(kbEnd); break;
                case 11: ev.KeyDown = Key(kbF1); break;
                case 12: ev.KeyDown = Key(kbF2); break;
                case 13: ev.KeyDown = Key(kbF3); break;
                case 14: ev.KeyDown = Key(kbF4); break;
                case 15: ev.KeyDown = Key(kbF5); break;
                case 17: ev.KeyDown = Key(kbF6); break;
                case 18: ev.KeyDown = Key(kbF7); break;
                case 19: ev.KeyDown = Key(kbF8); break;
                case 20: ev.KeyDown = Key(kbF9); break;
                case 21: ev.KeyDown = Key(kbF10); break;
                // UPSTREAM: upstream treats 23 and 24 as Shift+F1/F2 (Putty), since ncurses
                // already maps the terminfo F11/F12. Without terminfo, follow XTerm.
                case 23: ev.KeyDown = Key(kbF11); break;
                case 24: ev.KeyDown = Key(kbF12); break;
                case 25: ev.KeyDown = Key(kbShiftF3, kbShift); break;
                case 26: ev.KeyDown = Key(kbShiftF4, kbShift); break;
                case 28: ev.KeyDown = Key(kbShiftF5, kbShift); break;
                case 29: ev.KeyDown = Key(kbShiftF6, kbShift); break;
                case 31: ev.KeyDown = Key(kbShiftF7, kbShift); break;
                case 32: ev.KeyDown = Key(kbShiftF8, kbShift); break;
                case 33: ev.KeyDown = Key(kbShiftF9, kbShift); break;
                case 34: ev.KeyDown = Key(kbShiftF10, kbShift); break;
                case 200: state.BracketedPaste = true; return ParseResult.Ignored;
                case 201: state.BracketedPaste = false; return ParseResult.Ignored;
                default: return ParseResult.Rejected;
            }
        }
        else if (csi.Length == 1 && csi.GetValue(0) == 1)
        {
            if (!KeyFromLetter(terminator, XTermModDefault, out ev.KeyDown))
                return ParseResult.Rejected;
        }
        else if (csi.Length == 2 && csi.GetSeparator(0) == ';')
        {
            var mod = csi.GetValue(1);
            if (csi.GetValue(0) == 1)
            {
                if (!KeyFromLetter(terminator, mod, out ev.KeyDown))
                    return ParseResult.Rejected;
            }
            else if (terminator == '~')
            {
                ushort keyCode;
                switch (csi.GetValue(0))
                {
                    case 2: keyCode = kbIns; break;
                    case 3: keyCode = kbDel; break;
                    case 5: keyCode = kbPgUp; break;
                    case 6: keyCode = kbPgDn; break;
                    case 11: keyCode = kbF1; break;
                    case 12: keyCode = kbF2; break;
                    case 13: keyCode = kbF3; break;
                    case 14: keyCode = kbF4; break;
                    case 15: keyCode = kbF5; break;
                    case 17: keyCode = kbF6; break;
                    case 18: keyCode = kbF7; break;
                    case 19: keyCode = kbF8; break;
                    case 20: keyCode = kbF9; break;
                    case 21: keyCode = kbF10; break;
                    case 23: keyCode = kbF11; break;
                    case 24: keyCode = kbF12; break;
                    case 29: keyCode = kbNoKey; break; // Menu key (XTerm).
                    default: return ParseResult.Rejected;
                }
                ev.KeyDown = KeyWithXTermMods(keyCode, mod);
            }
            else
                return ParseResult.Rejected;
        }
        else if (csi.Length == 3 && csi.GetValue(0) == 27 && csi.GetSeparator(0) == ';' &&
                 csi.GetSeparator(1) == ';' && terminator == '~')
        {
            // XTerm's "modifyOtherKeys" mode.
            if (!KeyFromCodepoint(csi.GetValue(2), csi.GetValue(1), out ev.KeyDown))
                return ParseResult.Ignored;
        }
        else
            return ParseResult.Rejected;
        ev.What = evKeyDown;
        return ParseResult.Accepted;
    }

    // Pre: "\x1BO" has just been read.
    public static ParseResult ParseSS3Key(GetChBuf buf, ref TEvent ev)
    {
        // UPSTREAM: upstream rejects SS3 sequences without a modifier number, which ncurses
        // recognizes from terminfo (e.g. F1-F4, application mode arrows). Without curses,
        // they are parsed here with the default modifier.
        if (!buf.GetNum(out var mod))
            mod = XTermModDefault;
        var key = (uint)buf.Last();
        if (!KeyFromLetter(key, mod, out ev.KeyDown))
            return ParseResult.Rejected;
        ev.What = evKeyDown;
        return ParseResult.Accepted;
    }

    // https://sw.kovidgoyal.net/kitty/keyboard-protocol.html
    // Kitty events look like:
    // unicode-key-code : shifted-key-code : base-layout-key ; modifiers : event-type ; text-as-codepoints u
    public static ParseResult ParseKittyKey(CSIData csi, ref TEvent ev)
    {
        if (csi.Length < 1 || csi.Terminator != 'u')
            return ParseResult.Rejected;

        uint kittyShiftedKeyCode = 0, kittyBaseLayoutKey = 0, kittyModifiers = 1, kittyEventType = 1, kittyText = 0;
        var i = 0;
        var kittyKeyCode = csi.GetValue(i++, 0);
        if (i < csi.Length && csi.GetSeparator(i - 1) == ':')
            kittyShiftedKeyCode = csi.GetValue(i++, 0);
        if (i < csi.Length && csi.GetSeparator(i - 1) == ':')
            kittyBaseLayoutKey = csi.GetValue(i++, 0);
        if (i < csi.Length)
            kittyModifiers = csi.GetValue(i++, 1);
        if (i < csi.Length && csi.GetSeparator(i - 1) == ':')
            kittyEventType = csi.GetValue(i++, 1);
        if (i < csi.Length)
            kittyText = csi.GetValue(i++, 0); // Only the first character is supported.

        if (kittyEventType != 1)
            return ParseResult.Ignored; // Only press events are of interest.

        var codepoint = kittyKeyCode;
        if (kittyText != 0)
            codepoint = kittyText;
        else if (kittyShiftedKeyCode != 0)
            codepoint = kittyShiftedKeyCode;

        if (!KeyFromCodepoint(codepoint, kittyModifiers, out ev.KeyDown))
            return ParseResult.Ignored;

        // Let Ctrl/Alt shortcuts work with non-ASCII keyboard layouts (e.g. Ctrl+Ф in the RU
        // layout matches Ctrl+A), using the base layout key, which is always lowercase.
        if (!IsAsciiLetter(kittyKeyCode) && IsAsciiLetter(kittyBaseLayoutKey) &&
            (ev.KeyDown.ControlKeyState & (kbCtrlShift | kbAltShift)) != 0)
            ev.KeyDown.KeyCode = GetModdedKeyCode((ushort)(kittyBaseLayoutKey - 'a' + 'A'), ev.KeyDown.ControlKeyState);

        ev.What = evKeyDown;
        return ParseResult.Accepted;
    }

    // Pre: csi.Terminator == 'R'. A Cursor Position Report, in response to a Device Status
    // Report request.
    public static ParseResult ParseCPR(CSIData csi, InputState state)
    {
        if (csi.Length != 2 || csi.GetSeparator(0) != ';')
            return ParseResult.Rejected;
        state.GotDsrResponse = true;
        return ParseResult.Ignored;
    }

    // Skips a DCS or OSC string up to BEL or ST ("\x1B\\").
    private static bool ReadUntilBelOrSt(GetChBuf buf)
    {
        var input = buf.In;
        int k;
        while ((k = input.Get()) != -1)
        {
            if (k == '\x07')
                return true;
            if (k == '\x1B')
            {
                var k2 = input.Get();
                if (k2 == '\\')
                    return true;
                if (k2 != -1)
                    input.Unget(k2);
            }
        }
        return false;
    }
}
