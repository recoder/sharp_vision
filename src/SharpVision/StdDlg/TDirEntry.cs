namespace SharpVision.StdDlg;

/// <summary>A line of a <see cref="TDirListBox"/>: the text shown and the directory it stands for.</summary>
/// Ported from include/tvision/stddlg.h (class TDirEntry).
public class TDirEntry(string txt, string dir)
{
    /// <summary>The directory.</summary>
    public string Dir { get; } = dir;

    /// <summary>The text shown, with the tree graphics.</summary>
    public string Text { get; } = txt;
}
