using System;
using System.IO;

using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Dialogs.MsgBox;
using static SharpVision.Events.EventCodes;
using static SharpVision.StdDlg.Dir;
using static SharpVision.StdDlg.StdDlgCommands;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.StdDlg;

/// <summary>
/// The standard Change Directory dialog: a directory name line with history, a directory tree, and
/// OK, Chdir and Revert buttons. OK changes the process's current directory.
/// </summary>
/// Ported from source/tvision/tchdrdlg.cpp, source/tvision/tvtext2.cpp (texts),
/// include/tvision/stddlg.h (class TChDirDialog).
/// <remarks>The dialog transfers no data.</remarks>
public class TChDirDialog : TDialog
{
    /// <summary>Option to use the dialog immediately.</summary>
    public const ushort cdNormal = 0x0000;
    /// <summary>Do not load the current directory.</summary>
    public const ushort cdNoLoadDir = 0x0001;
    /// <summary>Put a help button in the dialog.</summary>
    public const ushort cdHelpButton = 0x0002;

    /// <summary>The title.</summary>
    public static string ChangeDirTitle = "Change Directory";
    /// <summary>The directory name label.</summary>
    public static string DirNameText = "Directory ~n~ame";
    /// <summary>The tree label.</summary>
    public static string DirTreeText = "Directory ~t~ree";
    /// <summary>The OK button.</summary>
    public static string OkText = "O~K~";
    /// <summary>The Chdir button.</summary>
    public static string ChdirText = "~C~hdir";
    /// <summary>The Revert button.</summary>
    public static string RevertText = "~R~evert";
    /// <summary>The Help button.</summary>
    public static string HelpText = "Help";
    /// <summary>The directory of the Drives line.</summary>
    public static string DrivesText = "Drives";
    /// <summary>The error for a directory that cannot be entered.</summary>
    public static string InvalidText = "Invalid directory";

    internal TInputLine? DirInput;
    internal TDirListBox? DirList;
    internal TButton? OkButton;
    internal TButton? ChDirButton;

    /// <summary>Creates the dialog with the options <paramref name="opts"/> and the history <paramref name="histId"/>.</summary>
    public TChDirDialog(ushort opts, ushort histId)
        : base(new TRect(16, 2, 64, 20), ChangeDirTitle)
    {
        Options |= ofCentered;
        Flags |= (byte)wfGrow;

        DirInput = new TInputLine(new TRect(3, 3, 42, 4), 259) { GrowMode = (byte)gfGrowHiX };
        Insert(DirInput);
        Insert(new TLabel(new TRect(2, 2, 17, 3), DirNameText, DirInput));
        Insert(new THistory(new TRect(42, 3, 45, 4), DirInput, histId) { GrowMode = gfGrowLoX | gfGrowHiX });

        var sb = new TScrollBar(new TRect(32, 6, 33, 16));
        Insert(sb);
        DirList = new TDirListBox(new TRect(3, 6, 32, 16), sb) { GrowMode = gfGrowHiX | gfGrowHiY };
        Insert(DirList);
        Insert(new TLabel(new TRect(2, 5, 17, 6), DirTreeText, DirList));

        OkButton = new TButton(new TRect(35, 6, 45, 8), OkText, cmOK, bfDefault) { GrowMode = gfGrowLoX | gfGrowHiX };
        Insert(OkButton);
        ChDirButton = new TButton(new TRect(35, 9, 45, 11), ChdirText, cmChangeDir, bfNormal) { GrowMode = gfGrowLoX | gfGrowHiX };
        Insert(ChDirButton);
        Insert(new TButton(new TRect(35, 12, 45, 14), RevertText, cmRevert, bfNormal) { GrowMode = gfGrowLoX | gfGrowHiX });
        if ((opts & cdHelpButton) != 0)
            Insert(new TButton(new TRect(35, 15, 45, 17), HelpText, cmHelp, bfNormal) { GrowMode = gfGrowLoX | gfGrowHiX });

        if ((opts & cdNoLoadDir) == 0)
            SetUpDialog();
        SelectNext(false);
    }

    /// <summary>No data slots.</summary>
    public override int DataSize() => 0;

    /// <inheritdoc />
    public override void ShutDown()
    {
        DirList = null;
        DirInput = null;
        OkButton = null;
        ChDirButton = null;
        base.ShutDown();
    }

    /// <summary>At least 48x18.</summary>
    public override void SizeLimits(out TPoint min, out TPoint max)
    {
        base.SizeLimits(out min, out max);
        min.X = 48;
        min.Y = 18;
    }

    /// <summary>Transfers nothing.</summary>
    public override void GetData(Span<object?> rec)
    {
    }

    /// <summary>Transfers nothing.</summary>
    public override void SetData(ReadOnlySpan<object?> rec)
    {
    }

    private static bool IsSeparator(char c) => c == '\\' || c == '/';

    private static string TrimEndSeparator(string path) =>
        path.Length > 3 && IsSeparator(path[^1]) ? path[..^1] : path;

    // The current directory with '\' separators; on Unix, without the emulated drive.
    private static string GetCurrentDir()
    {
        var dir = GetCurDir();
        return OperatingSystem.IsWindows() ? dir : dir[2..];
    }

    /// <summary>Revert shows the current directory again; Chdir shows the tree of the focused entry.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What != evCommand)
            return;
        string curDir;
        switch (ev.Message.Command)
        {
            case cmRevert:
                curDir = GetCurrentDir();
                break;
            case cmChangeDir:
                curDir = DirList!.List()![DirList.Focused].Dir;
                if (curDir == DrivesText)
                    break;
                if (IsSeparator(curDir[0]) || DriveValid(curDir[0]))
                {
                    if (!IsSeparator(curDir[^1]))
                        curDir += "\\";
                }
                else
                    return;
                break;
            default:
                return;
        }
        DirList!.NewDirectory(curDir);
        DirInput!.Data = TrimEndSeparator(curDir);
        DirInput.DrawView();
        DirList.Select();
        ClearEvent(ref ev);
    }

    /// <summary>Shows the current directory in the tree and the line.</summary>
    public void SetUpDialog()
    {
        if (DirList is null)
            return;
        var curDir = GetCurrentDir();
        DirList.NewDirectory(curDir);
        if (DirInput is not null)
        {
            DirInput.Data = TrimEndSeparator(curDir);
            DirInput.DrawView();
        }
    }

    /// <summary>On OK, changes the current directory to the expanded line, reporting failure.</summary>
    public override bool Valid(ushort command)
    {
        if (command != cmOK)
            return true;
        var path = TrimEndSeparator(FExpand(DirInput!.Data));
        try
        {
            Directory.SetCurrentDirectory(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox($"{InvalidText}: '{path}'.", mfError | mfOKButton);
            return false;
        }
        return true;
    }
}
