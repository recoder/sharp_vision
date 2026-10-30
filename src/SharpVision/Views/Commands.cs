namespace SharpVision.Views;

/// <summary>
/// Standard command codes. Commands 0-255 can be disabled through <c>TCommandSet</c>;
/// applications should define their own commands outside the ranges used here.
/// </summary>
/// Ported from include/tvision/views.h (cmXxx command codes).
public static class Commands
{
    // Standard command codes

    /// <summary>Command.</summary>
    public const ushort cmValid = 0;

    /// <summary>Command.</summary>
    public const ushort cmQuit = 1;

    /// <summary>Command.</summary>
    public const ushort cmError = 2;

    /// <summary>Command.</summary>
    public const ushort cmMenu = 3;

    /// <summary>Command.</summary>
    public const ushort cmClose = 4;

    /// <summary>Command.</summary>
    public const ushort cmZoom = 5;

    /// <summary>Command.</summary>
    public const ushort cmResize = 6;

    /// <summary>Command.</summary>
    public const ushort cmNext = 7;

    /// <summary>Command.</summary>
    public const ushort cmPrev = 8;

    /// <summary>Command.</summary>
    public const ushort cmHelp = 9;

    // TDialog standard commands

    /// <summary>Command.</summary>
    public const ushort cmOK = 10;

    /// <summary>Command.</summary>
    public const ushort cmCancel = 11;

    /// <summary>Command.</summary>
    public const ushort cmYes = 12;

    /// <summary>Command.</summary>
    public const ushort cmNo = 13;

    /// <summary>Command.</summary>
    public const ushort cmDefault = 14;

    // Standard application commands

    /// <summary>Command.</summary>
    public const ushort cmNew = 30;

    /// <summary>Command.</summary>
    public const ushort cmOpen = 31;

    /// <summary>Command.</summary>
    public const ushort cmSave = 32;

    /// <summary>Command.</summary>
    public const ushort cmSaveAs = 33;

    /// <summary>Command.</summary>
    public const ushort cmSaveAll = 34;

    /// <summary>Command.</summary>
    public const ushort cmChDir = 35;

    /// <summary>Command.</summary>
    public const ushort cmDosShell = 36;

    /// <summary>Command.</summary>
    public const ushort cmCloseAll = 37;

    // Application command codes

    /// <summary>Command.</summary>
    public const ushort cmCut = 20;

    /// <summary>Command.</summary>
    public const ushort cmCopy = 21;

    /// <summary>Command.</summary>
    public const ushort cmPaste = 22;

    /// <summary>Command.</summary>
    public const ushort cmUndo = 23;

    /// <summary>Command.</summary>
    public const ushort cmClear = 24;

    /// <summary>Command.</summary>
    public const ushort cmTile = 25;

    /// <summary>Command.</summary>
    public const ushort cmCascade = 26;

    /// <summary>Command.</summary>
    public const ushort cmRedo = 27;

    // Standard messages

    /// <summary>Command.</summary>
    public const ushort cmReceivedFocus = 50;

    /// <summary>Command.</summary>
    public const ushort cmReleasedFocus = 51;

    /// <summary>Command.</summary>
    public const ushort cmCommandSetChanged = 52;

    /// <summary>Command.</summary>
    public const ushort cmTimerExpired = 58;

    // TScrollBar messages

    /// <summary>Command.</summary>
    public const ushort cmScrollBarChanged = 53;

    /// <summary>Command.</summary>
    public const ushort cmScrollBarClicked = 54;

    // TWindow select messages

    /// <summary>Command.</summary>
    public const ushort cmSelectWindowNum = 55;

    // TListViewer messages

    /// <summary>Command.</summary>
    public const ushort cmListItemSelected = 56;

    // TProgram messages

    /// <summary>Command.</summary>
    public const ushort cmScreenChanged = 57;
}
