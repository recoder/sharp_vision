namespace SharpVision.ColorSel;

/// <summary>A named palette entry in a linked list of items.</summary>
/// Ported from source/tvision/colorsel.cpp, include/tvision/colorsel.h (class TColorItem).
public class TColorItem(string nm, byte idx, TColorItem? nxt = null)
{
    /// <summary>The name shown in the item list.</summary>
    public string Name = nm;

    /// <summary>The palette index the item edits.</summary>
    public byte Index = idx;

    /// <summary>The next item, or null.</summary>
    public TColorItem? Next = nxt;

    /// <summary>Appends <paramref name="i2"/> to the end of the list that starts at <paramref name="i1"/>.</summary>
    public static TColorItem operator +(TColorItem i1, TColorItem i2)
    {
        var cur = i1;
        while (cur.Next is not null)
            cur = cur.Next;
        cur.Next = i2;
        return i1;
    }
}
