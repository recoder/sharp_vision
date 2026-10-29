// Ported from include/tvision/scrncell.h (struct TScreenCell).

using System;

namespace SharpVision.Drawing;

/// <summary>
/// The text and color attribute of a screen cell. <c>default(TScreenCell)</c> is an empty
/// cell with default colors and no style.
/// </summary>
public struct TScreenCell : IEquatable<TScreenCell>
{
    /// <summary>The cell's text.</summary>
    public TScreenCharacter Character;

    /// <summary>The cell's colors and style.</summary>
    public TColorAttr Attribute;

    /// <summary>Creates a cell from text and an attribute.</summary>
    public TScreenCell(TScreenCharacter character, TColorAttr attribute)
    {
        Character = character;
        Attribute = attribute;
    }

    /// <summary>
    /// Creates a cell from a DOS cell value: the low byte is a code page character (CP437 by
    /// default) and the high byte a BIOS attribute.
    /// </summary>
    /// <remarks>
    /// UPSTREAM: upstream stores the raw byte and translates it when displaying it. Cells hold
    /// Unicode text in this port, so the byte is translated here.
    /// </remarks>
    public TScreenCell(ushort bios)
    {
        Character = default;
        Character.InitWithText(CpTranslator.ToUnicode((byte)bios));
        Attribute = bios >> 8;
    }

    /// <inheritdoc />
    public static bool operator ==(TScreenCell a, TScreenCell b) => a.Equals(b);

    /// <inheritdoc />
    public static bool operator !=(TScreenCell a, TScreenCell b) => !a.Equals(b);

    /// <inheritdoc />
    public readonly bool Equals(TScreenCell other) =>
        Character == other.Character && Attribute == other.Attribute;

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is TScreenCell other && Equals(other);

    /// <inheritdoc />
    public override readonly int GetHashCode() => HashCode.Combine(Character, Attribute);

    /// <inheritdoc />
    public override readonly string ToString() => $"{Character} {Attribute}";
}
