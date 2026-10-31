using System;
using System.Linq;

using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.StdDlg.Dir;
using static SharpVision.StdDlg.StdDlgCommands;

namespace SharpVision.StdDlg;

/// <summary>
/// A two-column sorted list of the files matching a wildcard plus the subdirectories, with directories
/// shown as <c>name\</c>. Focusing an entry broadcasts <c>cmFileFocused</c>; double-clicking broadcasts
/// <c>cmFileDoubleClicked</c>. Holding Shift (or typing a leading dot) searches the directories.
/// </summary>
/// Ported from source/tvision/tfillist.cpp, include/tvision/stddlg.h (class TFileList).
/// <remarks>The items are a <see cref="TFileCollection"/>. The list transfers no data.</remarks>
public class TFileList : TSortedListBox
{
    private static readonly TSearchRec _noFile = new();

    /// <summary>Creates an empty two-column file list.</summary>
    public TFileList(TRect bounds, TScrollBar? aScrollBar) : base(bounds, 2, aScrollBar)
    {
    }

    /// <summary>Focuses <paramref name="item"/> and broadcasts <c>cmFileFocused</c> with its entry.</summary>
    public override void FocusItem(int item)
    {
        base.FocusItem(item);
        Message(Owner, evBroadcast, cmFileFocused, List()![item]);
    }

    /// <summary>Broadcasts <c>cmFileDoubleClicked</c> with the entry.</summary>
    public override void SelectItem(int item) => Message(Owner, evBroadcast, cmFileDoubleClicked, List()![item]);

    /// <summary>Transfers nothing.</summary>
    public override void GetData(Span<object?> rec)
    {
    }

    /// <summary>Transfers nothing.</summary>
    public override void SetData(ReadOnlySpan<object?> rec)
    {
    }

    /// <summary>No data slots.</summary>
    public override int DataSize() => 0;

    /// <summary>A search key: a directory entry when Shift was held or the text starts with a dot.</summary>
    protected override object GetKey(string s) => new TSearchRec
    {
        Attr = (ShiftState & kbShift) != 0 || s.StartsWith('.') ? faDirec : (byte)0,
        Name = s,
    };

    /// <inheritdoc cref="TFileCollection.Compare"/>
    protected override int Compare(object item, object key) => TFileCollection.Compare((TSearchRec)item, (TSearchRec)key);

    /// <summary>The name of <paramref name="item"/>, truncated to <paramref name="maxLen"/>, with <c>\</c> after a directory.</summary>
    public override string GetText(int item, int maxLen)
    {
        var f = List()![item];
        var dest = f.Name.Length > maxLen ? f.Name[..maxLen] : f.Name;
        if ((f.Attr & faDirec) != 0)
            dest += "\\";
        return dest;
    }

    /// <summary>The entries, or null.</summary>
    public new TFileCollection? List() => (TFileCollection?)base.List();

    /// <summary>Lists <paramref name="wildCard"/> in <paramref name="dir"/>.</summary>
    public void ReadDirectory(string dir, string wildCard) => ReadDirectory(dir + wildCard);

    /// <summary>
    /// Lists the files matching <paramref name="aWildCard"/> (a path with a wildcard), the subdirectories of its
    /// directory except hidden ones, and <c>..</c> unless it is the root; then broadcasts <c>cmFileFocused</c>.
    /// </summary>
    public void ReadDirectory(string aWildCard)
    {
        const int findAttr = faRdOnly | faArch;
        var path = aWildCard;
        var fileList = new TFileCollection();

        foreach (var s in FindFirst(path, findAttr))
        {
            if ((s.Attr & faDirec) == 0)
                fileList.Insert(s);
        }

        path = FExpand(path);
        FnSplit(path, out var drive, out var dir, out _, out _);
        path = FnMerge(drive, dir, "*", ".*");
        foreach (var s in FindFirst(path, faDirec))
        {
            if ((s.Attr & faDirec) != 0 && s.Name[0] != '.')
                fileList.Insert(s);
        }

        if (dir.Length > 1)
        {
            path = FnMerge(drive, dir, "..", null);
            var p = FindFirst(path, faDirec).FirstOrDefault();
            if (p is not null)
                p.Name = ".."; // findfirst returns the actual directory name.
            else
                p = new TSearchRec { Name = "..", Size = 0, Time = 0x210000, Attr = faDirec };
            fileList.Insert(p);
        }

        // Upstream's "Too many files." warning is for running out of memory, which cannot happen here.
        NewList(fileList);
        Message(Owner, evBroadcast, cmFileFocused, fileList.Count > 0 ? fileList[0] : _noFile);
    }
}
