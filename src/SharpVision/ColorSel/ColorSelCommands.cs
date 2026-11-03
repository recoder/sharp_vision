namespace SharpVision.ColorSel;

/// <summary>The commands the color dialog's views broadcast to each other.</summary>
/// Ported from include/tvision/colorsel.h (__COLOR_COMMAND_CODES).
public static class ColorSelCommands
{
    /// <summary>The foreground color changed; the color is in <c>InfoByte</c>.</summary>
    public const ushort cmColorForegroundChanged = 71;
    /// <summary>The background color changed; the color is in <c>InfoByte</c>.</summary>
    public const ushort cmColorBackgroundChanged = 72;
    /// <summary>A new attribute to show; the BIOS attribute is in <c>InfoByte</c>.</summary>
    public const ushort cmColorSet = 73;
    /// <summary>A new group is focused; the <see cref="TColorGroup"/> is in <c>InfoPtr</c>.</summary>
    public const ushort cmNewColorItem = 74;
    /// <summary>A new item is focused; its palette index is in <c>InfoByte</c>.</summary>
    public const ushort cmNewColorIndex = 75;
    /// <summary>Remember the focused item of the focused group; the item is in <c>InfoByte</c>.</summary>
    public const ushort cmSaveColorIndex = 76;
}
