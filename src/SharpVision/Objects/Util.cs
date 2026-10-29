// Ported from include/tvision/util.h, source/tvision/drivers2.cpp (cstrlen, strwidth),
// source/tvision/tinputli.cpp (hotKey, hotKeyStr) and source/tvision/tvtext2.cpp
// (getAltChar, getAltCode, getCtrlChar, getCtrlCode).

using System;

using SharpVision.Drawing;
using SharpVision.Events;

using static SharpVision.Events.KeyCodes;

namespace SharpVision.Objects;

/// <summary>Helper functions for hotkey text (<c>"~F~ile"</c>) and Alt/Ctrl key codes.</summary>
public static class Util
{
    private const string AltCodes1 = "QWERTYUIOP\0\0\0\0ASDFGHJKL\0\0\0\0\0ZXCVBNM";
    private const string AltCodes2 = "1234567890-=";
    private const char AltSpaceChar = '≡'; // '\xF0' in CP437.
    private const string CtrlCodes = "\0ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>The display width of <paramref name="text"/>, ignoring <c>'~'</c> characters.</summary>
    public static int Cstrlen(ReadOnlySpan<char> text)
    {
        int i = 0, width = 0;
        while (i < text.Length)
        {
            if (text[i] != '~')
                TText.Next(text, ref i, ref width);
            else
                ++i;
        }
        return width;
    }

    /// <summary>The display width of <paramref name="text"/>.</summary>
    public static int Strwidth(ReadOnlySpan<char> text) => TText.Width(text);

    /// <summary>The text between the first pair of <c>'~'</c> in <paramref name="s"/> (to the end if unclosed), or empty.</summary>
    public static ReadOnlySpan<char> HotKeyStr(ReadOnlySpan<char> s)
    {
        var begin = s.IndexOf('~');
        if (begin < 0)
            return [];
        var rest = s[(begin + 1)..];
        var end = rest.IndexOf('~');
        return end < 0 ? rest : rest[..end];
    }

    /// <summary>The uppercase hotkey character of <paramref name="s"/>, or <c>'\0'</c>.</summary>
    public static char HotKey(ReadOnlySpan<char> s)
    {
        var p = HotKeyStr(s);
        return p.IsEmpty ? '\0' : char.ToUpperInvariant(p[0]);
    }

    /// <summary>The character of an Alt+key code, or <c>'\0'</c>.</summary>
    public static char GetAltChar(ushort keyCode)
    {
        var altChar = GetAltCharStr(new KeyDownEvent { KeyCode = keyCode });
        return altChar.Length == 0 ? '\0' : altChar[0];
    }

    /// <summary>The text of an Alt+key event: its text if it has any, otherwise the character mapped from its scan code.</summary>
    public static string GetAltCharStr(in KeyDownEvent keyDown)
    {
        // If the Alt+Key event contains text, return it.
        if ((keyDown.ControlKeyState & kbAltShift) != 0 && !keyDown.GetText().IsEmpty)
            return keyDown.Text!;

        // Otherwise, try mapping the scan code to a character.
        if (keyDown.CharScan.CharCode == 0)
        {
            var scanCode = keyDown.CharScan.ScanCode;
            var ch = '\0';
            if (keyDown.KeyCode == kbAltSpace)
                ch = AltSpaceChar; // Special case to handle Alt+Space.
            else if (scanCode is >= 0x10 and <= 0x32)
                ch = AltCodes1[scanCode - 0x10]; // Alt+letter.
            else if (scanCode is >= 0x78 and <= 0x83)
                ch = AltCodes2[scanCode - 0x78]; // Alt+number.
            if (ch != '\0')
                return ch.ToString();
        }
        return "";
    }

    /// <summary>The Alt+key code for character <paramref name="c"/>, or 0.</summary>
    public static ushort GetAltCode(char c)
    {
        if (c == 0)
            return 0;
        c = char.ToUpperInvariant(c);
        if (c == AltSpaceChar)
            return kbAltSpace; // Special case to handle Alt+Space.
        var i = AltCodes1.IndexOf(c);
        if (i >= 0)
            return (ushort)((i + 0x10) << 8);
        i = AltCodes2.IndexOf(c);
        if (i >= 0)
            return (ushort)((i + 0x78) << 8);
        return 0;
    }

    /// <summary>The letter of a Ctrl+key code, or <c>'\0'</c>.</summary>
    public static char GetCtrlChar(ushort keyCode)
    {
        var ctrlChar = GetCtrlCharStr(new KeyDownEvent { KeyCode = keyCode });
        return ctrlChar.Length == 0 ? '\0' : ctrlChar[0];
    }

    /// <summary>The text of a Ctrl+key event: its text if it has any, otherwise the letter of its control code.</summary>
    public static string GetCtrlCharStr(in KeyDownEvent keyDown)
    {
        // The Alt modifier must not be present, since that would be an Alt+Key press.
        if ((keyDown.ControlKeyState & kbAltShift) == 0 &&
            (keyDown.ControlKeyState & kbCtrlShift) != 0 &&
            !keyDown.GetText().IsEmpty)
            return keyDown.Text!;

        // Otherwise, map control characters into the corresponding ASCII letter.
        var charCode = keyDown.CharScan.CharCode;
        if (charCode > 0 && charCode <= 'Z' - 'A' + 1)
            return CtrlCodes[charCode].ToString();
        return "";
    }

    /// <summary>The Ctrl+key code for character <paramref name="ch"/>.</summary>
    public static ushort GetCtrlCode(char ch) =>
        (ushort)(GetAltCode(ch) | ((ch is >= 'a' and <= 'z' ? ch & ~0x20 : ch) - 'A' + 1));
}
