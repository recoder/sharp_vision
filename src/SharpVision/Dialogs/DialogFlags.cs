namespace SharpVision.Dialogs;

/// <summary>
/// Constants from <c>dialogs.h</c>: <see cref="TButton"/> flags, <see cref="TDialog"/> palettes and
/// <see cref="TMultiCheckBoxes"/> flags.
/// </summary>
/// Ported from include/tvision/dialogs.h (button flags, cmRecordHistory, dialog palettes,
/// TMultiCheckBoxes flags).
public static class DialogFlags
{
    // TButton flags

    /// <summary>Button flag: an ordinary button.</summary>
    public const ushort bfNormal = 0x00;

    /// <summary>Button flag: the default button, pressed by Enter.</summary>
    public const ushort bfDefault = 0x01;

    /// <summary>Button flag: the title is left-justified instead of centered.</summary>
    public const ushort bfLeftJust = 0x02;

    /// <summary>Button flag: pressing broadcasts the command to the owner instead of putting an event.</summary>
    public const ushort bfBroadcast = 0x04;

    /// <summary>Button flag: clicking the button focuses it.</summary>
    public const ushort bfGrabFocus = 0x08;

    /// <summary>Broadcast by a button when pressed, so input lines record their history.</summary>
    public const ushort cmRecordHistory = 60;

    // TDialog palettes

    /// <summary>Dialog palette.</summary>
    public const ushort dpBlueDialog = 0;

    /// <summary>Dialog palette.</summary>
    public const ushort dpCyanDialog = 1;

    /// <summary>Dialog palette.</summary>
    public const ushort dpGrayDialog = 2;

    // TMultiCheckBoxes flags: the high byte is the bits per item, the low byte the item mask.

    /// <summary>Multi-check-box flag: one bit per item.</summary>
    public const ushort cfOneBit = 0x0101;

    /// <summary>Multi-check-box flag: two bits per item.</summary>
    public const ushort cfTwoBits = 0x0203;

    /// <summary>Multi-check-box flag: four bits per item.</summary>
    public const ushort cfFourBits = 0x040F;

    /// <summary>Multi-check-box flag: eight bits per item.</summary>
    public const ushort cfEightBits = 0x08FF;
}
