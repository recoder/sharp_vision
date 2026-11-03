namespace SharpVision.ColorSel;

/// <summary>A named list of <see cref="TColorItem"/>s, itself in a linked list of groups.</summary>
/// Ported from source/tvision/colorsel.cpp, include/tvision/colorsel.h (class TColorGroup).
public class TColorGroup(string nm, TColorItem? itm = null, TColorGroup? nxt = null)
{
    /// <summary>The name shown in the group list.</summary>
    public string Name = nm;

    /// <summary>The item last focused in this group.</summary>
    public byte Index;

    /// <summary>The first item, or null.</summary>
    public TColorItem? Items = itm;

    /// <summary>The next group, or null.</summary>
    public TColorGroup? Next = nxt;

    /// <summary>Appends <paramref name="i"/> to the items of the last group in the list.</summary>
    public static TColorGroup operator +(TColorGroup g, TColorItem i)
    {
        var grp = g;
        while (grp.Next is not null)
            grp = grp.Next;

        if (grp.Items is null)
            grp.Items = i;
        else
        {
            var cur = grp.Items;
            while (cur.Next is not null)
                cur = cur.Next;
            cur.Next = i;
        }
        return g;
    }

    /// <summary>Appends <paramref name="g2"/> to the end of the list that starts at <paramref name="g1"/>.</summary>
    public static TColorGroup operator +(TColorGroup g1, TColorGroup g2)
    {
        var cur = g1;
        while (cur.Next is not null)
            cur = cur.Next;
        cur.Next = g2;
        return g1;
    }
}
