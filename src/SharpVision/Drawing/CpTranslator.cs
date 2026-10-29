// Ported from source/platform/codepage.cpp and include/tvision/internal/codepage.h
// (class CpTranslator).

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace SharpVision.Drawing;

/// <summary>
/// Translates between code page characters (by default CP437) and Unicode text.
/// </summary>
internal static class CpTranslator
{
    private static readonly string[] _cp437ToUnicode =
    [
        "\0", "☺", "☻", "♥", "♦", "♣", "♠", "•", "◘", "○", "◙", "♂", "♀", "♪", "♫", "☼",
        "►", "◄", "↕", "‼", "¶", "§", "▬", "↨", "↑", "↓", "→", "←", "∟", "↔", "▲", "▼",
        " ", "!", "\"", "#", "$", "%", "&", "'", "(", ")", "*", "+", ",", "-", ".", "/",
        "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", ":", ";", "<", "=", ">", "?",
        "@", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O",
        "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "[", "\\", "]", "^", "_",
        "`", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o",
        "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "{", "|", "}", "~", "⌂",
        "Ç", "ü", "é", "â", "ä", "à", "å", "ç", "ê", "ë", "è", "ï", "î", "ì", "Ä", "Å",
        "É", "æ", "Æ", "ô", "ö", "ò", "û", "ù", "ÿ", "Ö", "Ü", "¢", "£", "¥", "₧", "ƒ",
        "á", "í", "ó", "ú", "ñ", "Ñ", "ª", "º", "¿", "⌐", "¬", "½", "¼", "¡", "«", "»",
        "░", "▒", "▓", "│", "┤", "╡", "╢", "╖", "╕", "╣", "║", "╗", "╝", "╜", "╛", "┐",
        "└", "┴", "┬", "├", "─", "┼", "╞", "╟", "╚", "╔", "╩", "╦", "╠", "═", "╬", "╧",
        "╨", "╤", "╥", "╙", "╘", "╒", "╓", "╫", "╪", "┘", "┌", "█", "▄", "▌", "▐", "▀",
        "α", "ß", "Γ", "π", "Σ", "σ", "µ", "τ", "Φ", "Θ", "Ω", "δ", "∞", "φ", "ε", "∩",
        "≡", "±", "≥", "≤", "⌠", "⌡", "÷", "≈", "°", "∙", "·", "√", "ⁿ", "²", "■", " ",
    ];

    private static readonly object _lock = new();
    private static string[] _cpToUnicode = _cp437ToUnicode;
    private static Dictionary<string, byte> _unicodeToCp = BuildReverseMap(_cp437ToUnicode);

    /// <summary>Returns the Unicode text of code page character <paramref name="c"/>.</summary>
    public static string ToUnicode(byte c) => _cpToUnicode[c];

    /// <summary>
    /// Returns the code page character for <paramref name="s"/> (a single character),
    /// or 0 if there is none.
    /// </summary>
    public static byte FromUnicode(ReadOnlySpan<char> s)
    {
        return _unicodeToCp.TryGetValue(s.ToString(), out var c) ? c : (byte)0;
    }

    /// <summary>
    /// Replaces the translation table. Entries that are null or empty map to <c>"\0"</c>.
    /// Passing null restores CP437.
    /// </summary>
    public static void SetTranslation(IReadOnlyList<string?>? translation)
    {
        if (translation is not null && translation.Count != 256)
            throw new ArgumentException("A translation table must have 256 entries.", nameof(translation));

        lock (_lock)
        {
            string[] table;
            if (translation is null)
                table = _cp437ToUnicode;
            else
            {
                table = new string[256];
                for (var i = 0; i < 256; ++i)
                {
                    var s = translation[i];
                    table[i] = string.IsNullOrEmpty(s) ? "\0" : s;
                }
            }
            _unicodeToCp = BuildReverseMap(table);
            _cpToUnicode = table;
        }
    }

    private static Dictionary<string, byte> BuildReverseMap(string[] table)
    {
        var map = new Dictionary<string, byte>(StringComparer.Ordinal);
        for (var i = 0; i < 256; ++i)
        {
            // Only the first character of each entry is used as the key, and the first
            // code page character mapping to it wins, as in upstream.
            var s = table[i];
            var length = Rune.DecodeFromUtf16(s, out _, out var consumed) == OperationStatus.Done ? consumed : 1;
            map.TryAdd(s[..length], (byte)i);
        }
        return map;
    }
}
