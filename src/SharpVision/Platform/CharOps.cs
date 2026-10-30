using System;
using System.Globalization;
using System.Text;

namespace SharpVision.Platform;

/// <summary>
/// Character operations used by <c>TText</c>: display width and lowercase conversion.
/// Backends may replace them (e.g. the Linux console, which has no zero-width characters).
/// </summary>
/// Ported from source/platform/platfchr.cpp (Platform::CharOps).
internal static class CharOps
{
    /// <summary>
    /// Returns the number of columns a code point occupies: 0 for combining and other
    /// zero-width characters, 1 or 2 for printable characters, -1 for non-printable ones.
    /// </summary>
    public static Func<int, int> Width { get; set; } = DefaultWidth;

    /// <summary>Converts a code point to lowercase.</summary>
    public static Func<int, int> ToLower { get; set; } = DefaultToLower;

    // UPSTREAM: upstream calls the C library's locale-dependent wcwidth() on Unix and
    // measures characters through the console API on Windows. This is a managed
    // approximation: Markus Kuhn's wcwidth (2007-05-26) with zero-width characters
    // taken from .NET's Unicode categories and common emoji blocks treated as wide.
    internal static int DefaultWidth(int wc)
    {
        if (wc == 0)
            return 0;
        if (wc < 0x20 || (wc >= 0x7F && wc < 0xA0))
            return -1;
        if (wc < 0x300)
            return 1;
        if (!Rune.IsValid(wc))
            return -1;

        var category = Rune.GetUnicodeCategory(new Rune(wc));
        if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.Format)
            return 0;
        if ((wc >= 0x1160 && wc <= 0x11FF) || wc == 0x200B)
            return 0; // Hangul medial vowels and final consonants; zero width space.

        return IsWide(wc) ? 2 : 1;
    }

    private static bool IsWide(int wc) =>
        wc >= 0x1100 &&
        (wc <= 0x115F ||
         wc == 0x2329 || wc == 0x232A ||
         (wc >= 0x2E80 && wc <= 0xA4CF && wc != 0x303F) ||
         (wc >= 0xAC00 && wc <= 0xD7A3) ||
         (wc >= 0xF900 && wc <= 0xFAFF) ||
         (wc >= 0xFE10 && wc <= 0xFE19) ||
         (wc >= 0xFE30 && wc <= 0xFE6F) ||
         (wc >= 0xFF00 && wc <= 0xFF60) ||
         (wc >= 0xFFE0 && wc <= 0xFFE6) ||
         (wc >= 0x1F300 && wc <= 0x1F64F) ||
         (wc >= 0x1F680 && wc <= 0x1F6FF) ||
         (wc >= 0x1F900 && wc <= 0x1F9FF) ||
         (wc >= 0x1FA70 && wc <= 0x1FAFF) ||
         (wc >= 0x20000 && wc <= 0x2FFFD) ||
         (wc >= 0x30000 && wc <= 0x3FFFD));

    private static int DefaultToLower(int wc) =>
        Rune.IsValid(wc) ? Rune.ToLowerInvariant(new Rune(wc)).Value : wc;
}
