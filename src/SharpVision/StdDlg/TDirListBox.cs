using System;
using System.Collections.Generic;

using SharpVision.Dialogs;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.StdDlg.Dir;
using static SharpVision.StdDlg.StdDlgCommands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.StdDlg;

/// <summary>
/// The tree of a <see cref="TChDirDialog"/>: the path from the root to the current directory, then its
/// subdirectories. The current directory is shown selected; choosing a line sends <c>cmChangeDir</c>.
/// </summary>
/// Ported from source/tvision/tdirlist.cpp, source/tvision/tvtext1.cpp (texts),
/// include/tvision/stddlg.h (class TDirListBox).
/// <remarks>
/// The items are a <c>List&lt;TDirEntry&gt;</c> (upstream's <c>TDirCollection</c>). Paths use <c>\</c>; on
/// Unix there is no drive and no <c>Drives</c> line. Subdirectories are listed in directory order, unsorted.
/// </remarks>
public class TDirListBox : TListBox
{
    private const int IndentSize = 2;

    /// <summary>The graphics before each directory on the path.</summary>
    public static string PathDir = "└─┬";
    /// <summary>The graphics before the first subdirectory.</summary>
    public static string FirstDir = "└┬─";
    /// <summary>The graphics before the other subdirectories.</summary>
    public static string MiddleDir = " ├─";
    /// <summary>The graphics before the last drive.</summary>
    public static string LastDir = " └─";
    /// <summary>The line (and directory name) that lists the drives.</summary>
    public static string Drives = "Drives";
    /// <summary>The characters patched to close the tree on the last line.</summary>
    public static string Graphics = "└├─";

    private string _dir = "";
    private int _cur;

    /// <summary>Creates an empty one-column tree.</summary>
    public TDirListBox(TRect bounds, TScrollBar? aScrollBar) : base(bounds, 1, aScrollBar)
    {
    }

    /// <summary>The text of <paramref name="item"/>, truncated to <paramref name="maxLen"/>.</summary>
    public override string GetText(int item, int maxLen)
    {
        var text = List()![item].Text;
        return text.Length > maxLen ? text[..maxLen] : text;
    }

    /// <summary>Sends <c>cmChangeDir</c> with the entry to the owner.</summary>
    public override void SelectItem(int item) => Message(Owner, evCommand, cmChangeDir, List()![item]);

    /// <summary>Only the current directory is shown selected.</summary>
    public override bool IsSelected(int item) => item == _cur;

    /// <summary>The entries, or null.</summary>
    public new List<TDirEntry>? List() => (List<TDirEntry>?)base.List();

    private void ShowDrives(List<TDirEntry> dirs)
    {
        var isFirst = true;
        var oldc = '0';
        for (var c = 'A'; c <= 'Z'; c++)
        {
            if (c < 'C' || DriveValid(c))
            {
                if (oldc != '0')
                {
                    dirs.Add(new TDirEntry((isFirst ? FirstDir : MiddleDir) + oldc, oldc + ":\\"));
                    isFirst = false;
                }
                if (c == GetDisk() + 'A')
                    _cur = dirs.Count;
                oldc = c;
            }
        }
        if (oldc != '0')
            dirs.Add(new TDirEntry(LastDir + oldc, oldc + ":\\"));
    }

    private void ShowDirs(List<TDirEntry> dirs)
    {
        var indent = IndentSize;

        // Show root directory.
        var end = _dir.IndexOf('\\', StringComparison.Ordinal);
        if (end < 0)
            return;
        end++;
        dirs.Add(new TDirEntry(PathDir + _dir[..end], _dir[..end]));
        var curDir = end;

        // Show directories up to the current one.
        while ((end = _dir.IndexOf('\\', curDir)) >= 0)
        {
            dirs.Add(new TDirEntry(new string(' ', indent) + PathDir + _dir[curDir..end], _dir[..end]));
            curDir = end + 1;
            indent += IndentSize;
        }
        _cur = dirs.Count - 1;

        // Show subdirectories.
        var path = _dir[..(_dir.LastIndexOf('\\') + 1)];
        var isFirst = true;
        foreach (var ff in FindFirst(path + "*.*", faDirec))
        {
            if ((ff.Attr & faDirec) != 0 && ff.Name[0] != '.')
            {
                dirs.Add(new TDirEntry(new string(' ', indent) + (isFirst ? FirstDir : MiddleDir) + ff.Name, path + ff.Name));
                isFirst = false;
            }
        }

        // Close the tree on the last line.
        var last = dirs[^1];
        var p = last.Text;
        var i = p.IndexOf(Graphics[0], StringComparison.Ordinal);
        if (i < 0)
        {
            i = p.IndexOf(Graphics[1], StringComparison.Ordinal);
            if (i >= 0)
                p = p[..i] + Graphics[0] + p[(i + 1)..];
        }
        else
            p = p[..(i + 1)] + Graphics[2] + Graphics[2] + p[(i + 3)..];
        dirs[^1] = new TDirEntry(p, last.Dir);
    }

    /// <summary>Shows the tree for <paramref name="str"/> (a <c>\</c>-separated directory ending with <c>\</c>), or the drives for <see cref="Drives"/>.</summary>
    public void NewDirectory(string str)
    {
        _dir = str;
        var dirs = new List<TDirEntry>();
        // Add 'Drives' entry.
        if (OperatingSystem.IsWindows())
            dirs.Add(new TDirEntry(Drives, Drives));
        if (str == Drives)
            ShowDrives(dirs);
        else
            ShowDirs(dirs);
        NewList(dirs);
        FocusItem(_cur);
    }

    /// <summary>While focused, makes the dialog's Chdir button the default.</summary>
    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);
        if ((aState & sfFocused) != 0)
            ((TChDirDialog)Owner!).ChDirButton?.MakeDefault(enable);
    }
}
