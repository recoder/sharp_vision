// Ported from include/tvision/colors.h (slXxx constants).

namespace SharpVision.Drawing;

/// <summary>
/// Text style flags for <see cref="TColorAttr.Style"/>.
/// </summary>
/// <remarks>magiblot/tvision extension (not part of Turbo Vision 2.0).</remarks>
public static class StyleFlags
{
    /// <summary>Bold.</summary>
    public const ushort slBold = 0x001;

    /// <summary>Italic.</summary>
    public const ushort slItalic = 0x002;

    /// <summary>Underline.</summary>
    public const ushort slUnderline = 0x004;

    /// <summary>Blink.</summary>
    public const ushort slBlink = 0x008;

    /// <summary>Reverse video. Prefer <see cref="TColorAttr.Reversed"/>.</summary>
    public const ushort slReverse = 0x010;

    /// <summary>Strikethrough.</summary>
    public const ushort slStrike = 0x020;

    /// <summary>Marks a cell as covered by a window shadow.</summary>
    public const ushort slWindowShadow = 0x200;
}
