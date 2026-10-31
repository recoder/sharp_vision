using System.Collections.Generic;

using static SharpVision.StdDlg.Dir;

namespace SharpVision.StdDlg;

/// <summary>The entries of a <see cref="TFileList"/>, kept sorted: files by name, then directories, then <c>..</c>.</summary>
/// Ported from source/tvision/tfilecol.cpp, include/tvision/stddlg.h (class TFileCollection).
public class TFileCollection : List<TSearchRec>
{
    private static readonly IComparer<TSearchRec> _comparer = Comparer<TSearchRec>.Create(Compare);

    /// <summary>Inserts <paramref name="item"/> in order, unless an entry that compares equal is already there.</summary>
    public void Insert(TSearchRec item)
    {
        var i = BinarySearch(item, _comparer);
        if (i < 0)
            Insert(~i, item);
    }

    /// <summary>Orders two entries: equal names first, then <c>..</c> last, directories after files, and by name.</summary>
    public static int Compare(TSearchRec key1, TSearchRec key2)
    {
        if (string.CompareOrdinal(key1.Name, key2.Name) == 0)
            return 0;

        if (key1.Name == "..")
            return 1;
        if (key2.Name == "..")
            return -1;

        if ((key1.Attr & faDirec) != 0 && (key2.Attr & faDirec) == 0)
            return 1;
        if ((key2.Attr & faDirec) != 0 && (key1.Attr & faDirec) == 0)
            return -1;

        return string.CompareOrdinal(key1.Name, key2.Name);
    }
}
