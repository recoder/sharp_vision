// Ported from include/tvision/scrncell.h (struct TScreenCharacter).

using System;

namespace SharpVision.Drawing;

/// <summary>
/// The text in a screen cell. Usually written through <see cref="TText"/> rather than directly.
/// </summary>
/// <remarks>
/// <para>Holds one of:</para>
/// <list type="number">
/// <item>A single UTF-16 code unit (1 column wide).</item>
/// <item>One character plus any combining characters appended to it, up to
/// <see cref="MaxTextLength"/> UTF-16 code units (1 or 2 columns wide in total).</item>
/// <item>A wide char trail: the right half of the preceding double-width character.</item>
/// </list>
/// <para>
/// A double-width character is only displayed whole when the next cell is a wide char trail.
/// <c>default(TScreenCharacter)</c> is an empty cell (the text <c>"\0"</c>).
/// </para>
/// <para>
/// UPSTREAM: upstream stores up to 15 bytes of UTF-8; this port stores up to
/// <see cref="MaxTextLength"/> UTF-16 code units.
/// </para>
/// </remarks>
public struct TScreenCharacter : IEquatable<TScreenCharacter>
{
    /// <summary>Maximum text length, in UTF-16 code units, including combining characters.</summary>
    public const int MaxTextLength = 15;

    private const byte FWide = 0x1;
    private const byte FTrail = 0x2;
    private const byte FOverflow = 0x4;

    // Text is in _ch when _text is null; otherwise _text holds it and _ch is '\0'.
    private char _ch;
    private string? _text;
    private byte _flags;

    /// <summary>Creates a character holding <paramref name="c"/>.</summary>
    public TScreenCharacter(char c)
    {
        _ch = c;
        _text = null;
        _flags = 0;
    }

    /// <summary>Whether this is a double-width character.</summary>
    public readonly bool IsWide => (_flags & FWide) != 0;

    /// <summary>Whether this is the trail (right half) of a double-width character.</summary>
    public readonly bool IsWideCharTrail => (_flags & FTrail) != 0;

    /// <summary>Whether the text is a single UTF-16 code unit, available through <see cref="SingleChar"/>.</summary>
    public readonly bool IsSingleChar => _text is null;

    /// <summary>The text when <see cref="IsSingleChar"/>; otherwise <c>'\0'</c>.</summary>
    public readonly char SingleChar => _ch;

    /// <summary>Sets the text to <paramref name="c"/> and clears all flags.</summary>
    public void InitWithChar(char c) => this = new TScreenCharacter(c);

    /// <summary>
    /// Sets the text to a single (possibly surrogate pair) character and clears all flags.
    /// If <paramref name="text"/> is empty or longer than two code units, the cell becomes empty.
    /// </summary>
    public void InitWithText(ReadOnlySpan<char> text, bool wide = false)
    {
        this = default;
        if (text.Length is > 0 and <= 2)
        {
            if (wide)
                _flags |= FWide;
            if (text.Length == 1)
                _ch = text[0];
            else
                _text = text.ToString();
        }
    }

    /// <summary>Marks the cell as the trail of a double-width character and clears the text.</summary>
    public void InitAsWideCharTrail()
    {
        this = default;
        _flags = FTrail;
    }

    /// <summary>
    /// Appends a zero-width (combining) character. Once the text would exceed
    /// <see cref="MaxTextLength"/>, this and every later append are ignored.
    /// An empty cell (<c>'\0'</c>) is turned into a space first.
    /// </summary>
    public void AppendZeroWidthChar(ReadOnlySpan<char> mbc)
    {
        if ((_flags & FOverflow) != 0)
            return;
        var current = _text ?? (_ch == '\0' ? " " : _ch.ToString());
        if (mbc.Length <= MaxTextLength - current.Length)
        {
            _text = string.Concat(current, mbc);
            _ch = '\0';
        }
        else
            _flags |= FOverflow;
    }

    /// <summary>Returns the text. Not meaningful for a wide char trail.</summary>
    public readonly string GetText() => _text ?? _ch.ToString();

    /// <summary>Converts a character.</summary>
    public static implicit operator TScreenCharacter(char c) => new(c);

    /// <inheritdoc />
    public static bool operator ==(TScreenCharacter a, TScreenCharacter b) => a.Equals(b);

    /// <inheritdoc />
    public static bool operator !=(TScreenCharacter a, TScreenCharacter b) => !a.Equals(b);

    /// <inheritdoc />
    public readonly bool Equals(TScreenCharacter other) =>
        _ch == other._ch && _flags == other._flags && string.Equals(_text, other._text, StringComparison.Ordinal);

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is TScreenCharacter other && Equals(other);

    /// <inheritdoc />
    public override readonly int GetHashCode() => HashCode.Combine(_ch, _text, _flags);

    /// <inheritdoc />
    public override readonly string ToString() => IsWideCharTrail ? "<trail>" : GetText();
}
