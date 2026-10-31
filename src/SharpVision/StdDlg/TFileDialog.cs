using System;

using SharpVision.App;
using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Dialogs.MsgBox;
using static SharpVision.Events.EventCodes;
using static SharpVision.Objects.Util;
using static SharpVision.StdDlg.Dir;
using static SharpVision.StdDlg.StdDlgCommands;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.StdDlg;

/// <summary>
/// The standard Open/Save dialog: a file name line with history, a list of the files matching the wildcard
/// and the subdirectories, an info pane, and the buttons chosen by the <c>fdXxx</c> options. Typing a
/// wildcard or a directory and pressing a button changes the listing instead of closing the dialog.
/// </summary>
/// Ported from source/tvision/tfildlg.cpp, source/tvision/tvtext2.cpp (texts),
/// include/tvision/stddlg.h (class TFileDialog).
/// <remarks>The data slot is the file name: set a string, read back the expanded path.</remarks>
public class TFileDialog : TDialog
{
    /// <summary>Put an OK button in the dialog.</summary>
    public const ushort fdOKButton = 0x0001;
    /// <summary>Put an Open button in the dialog.</summary>
    public const ushort fdOpenButton = 0x0002;
    /// <summary>Put a Replace button in the dialog.</summary>
    public const ushort fdReplaceButton = 0x0004;
    /// <summary>Put a Clear button in the dialog.</summary>
    public const ushort fdClearButton = 0x0008;
    /// <summary>Put a Help button in the dialog.</summary>
    public const ushort fdHelpButton = 0x0010;
    /// <summary>Do not load the current directory.</summary>
    public const ushort fdNoLoadDir = 0x0100;

    /// <summary>The file list label.</summary>
    public static string FilesText = "~F~iles";
    /// <summary>The Open button.</summary>
    public static string OpenText = "~O~pen";
    /// <summary>The OK button.</summary>
    public static string OkText = "O~K~";
    /// <summary>The Replace button.</summary>
    public static string ReplaceText = "~R~eplace";
    /// <summary>The Clear button.</summary>
    public static string ClearText = "~C~lear";
    /// <summary>The Cancel button.</summary>
    public static string CancelText = "Cancel";
    /// <summary>The Help button.</summary>
    public static string HelpText = "~H~elp";
    /// <summary>The error for a directory that does not exist.</summary>
    public static string InvalidDriveText = "Invalid drive or directory";
    /// <summary>The error for an invalid file name.</summary>
    public static string InvalidFileText = "Invalid file name";

    /// <summary>The file name line, or null after shutdown.</summary>
    public TFileInputLine? FileName;

    /// <summary>The file list, or null after shutdown.</summary>
    public TFileList? FileList;

    /// <summary>The wildcard of the listing, e.g. <c>*.*</c>.</summary>
    public string WildCard;

    /// <summary>The directory of the listing, ending with a separator.</summary>
    public string Directory = "";

    /// <summary>
    /// Creates the dialog titled <paramref name="aTitle"/> listing <paramref name="aWildCard"/>, with the line
    /// labelled <paramref name="inputName"/>, the buttons in <paramref name="aOptions"/> and the history
    /// <paramref name="histId"/>. Unless <c>fdNoLoadDir</c> is given, lists the current directory.
    /// </summary>
    public TFileDialog(string aWildCard, string? aTitle, string inputName, ushort aOptions, byte histId)
        : base(new TRect(15, 1, 64, 20), aTitle)
    {
        Options |= ofCentered;
        Flags |= (byte)wfGrow;
        WildCard = aWildCard;
        FileName = new TFileInputLine(new TRect(3, 3, 31, 4), 260) { Data = WildCard };
        Insert(FileName);
        First()!.GrowMode = (byte)gfGrowHiX;
        Insert(new TLabel(new TRect(2, 2, 3 + Cstrlen(inputName), 3), inputName, FileName));
        First()!.GrowMode = 0;
        Insert(new THistory(new TRect(31, 3, 34, 4), FileName, histId));
        First()!.GrowMode = gfGrowLoX | gfGrowHiX;
        var sb = new TScrollBar(new TRect(3, 14, 34, 15));
        Insert(sb);
        Insert(FileList = new TFileList(new TRect(3, 6, 34, 14), sb));
        First()!.GrowMode = gfGrowHiX | gfGrowHiY;
        Insert(new TLabel(new TRect(2, 5, 8, 6), FilesText, FileList));
        First()!.GrowMode = 0;

        var opt = bfDefault;
        var r = new TRect(35, 3, 46, 5);
        void AddButton(string title, ushort command)
        {
            Insert(new TButton(r, title, command, opt));
            First()!.GrowMode = gfGrowLoX | gfGrowHiX;
            opt = bfNormal;
            r.A.Y += 3;
            r.B.Y += 3;
        }
        if ((aOptions & fdOpenButton) != 0)
            AddButton(OpenText, cmFileOpen);
        if ((aOptions & fdOKButton) != 0)
            AddButton(OkText, cmFileOpen);
        if ((aOptions & fdReplaceButton) != 0)
            AddButton(ReplaceText, cmFileReplace);
        if ((aOptions & fdClearButton) != 0)
            AddButton(ClearText, cmFileClear);
        opt = bfNormal;
        AddButton(CancelText, cmCancel);
        if ((aOptions & fdHelpButton) != 0)
            AddButton(HelpText, cmHelp);

        Insert(new TFileInfoPane(new TRect(1, 16, 48, 18)));
        First()!.GrowMode = gfGrowAll & ~gfGrowLoX;

        SelectNext(false);

        // I feel too lazy to update all the sizes above. The new default size
        // is set by resizing the dialog.
        var bounds = GetBounds();
        var screenSize = TProgram.Application!.Size;
        var screenBounds = TProgram.Application.GetBounds();
        if (screenSize.X > 90)
            bounds.Grow(15, 0); // New size 79
        else if (screenSize.X > 63)
        {
            screenBounds.Grow(-7, 0);
            bounds.A.X = screenBounds.A.X;
            bounds.B.X = screenBounds.B.X;
        }
        if (screenSize.Y > 34)
            bounds.Grow(0, 5); // New height 29
        else if (screenSize.Y > 25)
        {
            screenBounds.Grow(0, -3);
            bounds.A.Y = screenBounds.A.Y;
            bounds.B.Y = screenBounds.B.Y;
        }
        // Making the dialog greater than this does not make much sense
        // as it would be too sparse.
        Locate(ref bounds);

        if ((aOptions & fdNoLoadDir) == 0)
            ReadDirectory();
    }

    /// <inheritdoc />
    public override void ShutDown()
    {
        FileName = null;
        FileList = null;
        base.ShutDown();
    }

    /// <summary>At least 49x19.</summary>
    public override void SizeLimits(out TPoint min, out TPoint max)
    {
        base.SizeLimits(out min, out max);
        min.X = 49;
        min.Y = 19;
    }

    /// <summary>
    /// The file name line expanded against <see cref="Directory"/>; if it names no file, the wildcard's name
    /// and extension are used.
    /// </summary>
    public string GetFileName()
    {
        // Upstream's trim() only strips spaces in 16-bit builds.
        var buf = FExpand(FileName!.Data, Directory);
        FnSplit(buf, out var drive, out var path, out var name, out var ext);
        if (name.Length == 0 && ext.Length == 0)
        {
            FnSplit(WildCard, out _, out _, out var tName, out var tExt);
            buf = FnMerge(drive, path, tName, tExt);
        }
        return buf;
    }

    /// <summary>Ends the dialog on Open, Replace or Clear, and turns a double-clicked file into OK.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evCommand)
        {
            switch (ev.Message.Command)
            {
                case cmFileOpen:
                case cmFileReplace:
                case cmFileClear:
                    EndModal(ev.Message.Command);
                    ClearEvent(ref ev);
                    break;
            }
        }
        else if (ev.What == evBroadcast && ev.Message.Command == cmFileDoubleClicked)
        {
            ev.What = evCommand;
            ev.Message.Command = cmOK;
            PutEvent(ref ev);
            ClearEvent(ref ev);
        }
    }

    /// <summary>Lists <see cref="WildCard"/> in the current directory.</summary>
    public void ReadDirectory()
    {
        Directory = GetCurDir();
        FileList!.ReadDirectory(WildCard);
    }

    /// <summary>Sets the file name; a wildcard also changes the listing and selects the line.</summary>
    public override void SetData(ReadOnlySpan<object?> rec)
    {
        base.SetData(rec);
        if (rec[0] is string s && s.Length != 0 && IsWild(s))
        {
            Valid(cmFileInit);
            FileName!.Select();
        }
    }

    /// <summary>Writes <see cref="GetFileName"/> into the slot.</summary>
    public override void GetData(Span<object?> rec) => rec[0] = GetFileName();

    private bool CheckDirectory(string str)
    {
        if (PathValid(str))
            return true;
        MessageBox($"{InvalidDriveText}: '{str}'", mfError | mfOKButton);
        FileName!.Select();
        return false;
    }

    /// <summary>
    /// For a wildcard or a directory, changes the listing and refuses to close; accepts a valid file name;
    /// reports an invalid one. Cancel and Clear always close.
    /// </summary>
    public override bool Valid(ushort command)
    {
        if (command == 0)
            return true;
        if (!base.Valid(command))
            return false;
        if (command == cmCancel || command == cmFileClear)
            return true;

        var fName = GetFileName();
        if (IsWild(fName))
        {
            FnSplit(fName, out var drive, out var dir, out var name, out var ext);
            var path = drive + dir;
            if (CheckDirectory(path))
            {
                Directory = path;
                WildCard = name + ext;
                if (command != cmFileInit)
                    FileList!.Select();
                FileList!.ReadDirectory(Directory, WildCard);
            }
        }
        else if (IsDir(fName))
        {
            if (CheckDirectory(fName))
            {
                Directory = fName + "\\";
                if (command != cmFileInit)
                    FileList!.Select();
                FileList!.ReadDirectory(Directory, WildCard);
            }
        }
        else if (ValidFileName(fName))
            return true;
        else
        {
            MessageBox($"{InvalidFileText}: '{fName}'", mfError | mfOKButton);
            return false;
        }
        return false;
    }
}
