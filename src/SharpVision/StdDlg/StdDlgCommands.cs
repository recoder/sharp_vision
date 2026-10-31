namespace SharpVision.StdDlg;

/// <summary>The commands and messages of the file and directory dialogs.</summary>
/// Ported from include/tvision/stddlg.h (__FILE_CMDS).
public static class StdDlgCommands
{
    /// <summary>Returned from <c>TFileDialog</c> when Open is pressed.</summary>
    public const ushort cmFileOpen = 1001;
    /// <summary>Returned from <c>TFileDialog</c> when Replace is pressed.</summary>
    public const ushort cmFileReplace = 1002;
    /// <summary>Returned from <c>TFileDialog</c> when Clear is pressed.</summary>
    public const ushort cmFileClear = 1003;
    /// <summary>Used by <c>TFileDialog</c> internally.</summary>
    public const ushort cmFileInit = 1004;
    /// <summary>Changes the directory in <c>TChDirDialog</c>.</summary>
    public const ushort cmChangeDir = 1005;
    /// <summary>Used by <c>TChDirDialog</c> internally.</summary>
    public const ushort cmRevert = 1006;

    /// <summary>Broadcast: a new file was focused in the <see cref="TFileList"/>.</summary>
    public const ushort cmFileFocused = 102;
    /// <summary>Broadcast: a file was selected in the <see cref="TFileList"/>.</summary>
    public const ushort cmFileDoubleClicked = 103;
}
