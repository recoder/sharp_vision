namespace SharpVision.StdDlg;

/// <summary>A directory entry as found by <see cref="Dir.FindFirst"/>: attributes, DOS date and time, size and name.</summary>
/// Ported from include/tvision/stddlg.h (struct TSearchRec), source/tvision/tfillist.cpp (DirSearchRec).
public class TSearchRec
{
    /// <summary>The <c>faXxx</c> attributes (see <see cref="Dir"/>).</summary>
    public byte Attr { get; set; }

    /// <summary>The last write time in DOS format: the date in the high word, the time in the low word.</summary>
    public int Time { get; set; }

    /// <summary>The size in bytes, truncated to 32 bits as upstream does.</summary>
    public int Size { get; set; }

    /// <summary>The file name, without a directory.</summary>
    public string Name { get; set; } = "";
}
