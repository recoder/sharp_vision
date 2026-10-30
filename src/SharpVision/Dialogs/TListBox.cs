// Ported from source/tvision/tlistbox.cpp, include/tvision/dialogs.h (class TListBox).

using System;
using System.Collections.Generic;

using SharpVision.Objects;
using SharpVision.Views;

namespace SharpVision.Dialogs;

/// <summary>A list viewer over a list of strings, transferring a <see cref="TListBoxRec"/>.</summary>
/// <remarks>Palette layout: 1 = active, 2 = inactive, 3 = focused, 4 = selected, 5 = divider.</remarks>
public class TListBox : TListViewer
{
    /// <summary>The items, or null.</summary>
    protected List<string>? Items;

    /// <summary>Creates an empty list box with <paramref name="aNumCols"/> columns and an optional vertical scroll bar.</summary>
    public TListBox(TRect bounds, ushort aNumCols, TScrollBar? aScrollBar)
        : base(bounds, aNumCols, null, aScrollBar)
    {
        SetRange(0);
    }

    /// <summary>One data slot holding a <see cref="TListBoxRec"/>.</summary>
    public override int DataSize() => 1;

    /// <summary>Writes the items and the focused item into <paramref name="rec"/>.</summary>
    public override void GetData(Span<object?> rec) =>
        rec[0] = new TListBoxRec { Items = Items, Selection = (ushort)Focused };

    /// <summary>The text of <paramref name="item"/>, truncated to <paramref name="maxLen"/>.</summary>
    public override string GetText(int item, int maxLen)
    {
        if (Items is not null)
        {
            var s = Items[item];
            return s.Length > maxLen ? s[..maxLen] : s;
        }
        return "";
    }

    /// <summary>Replaces the items and focuses the first one.</summary>
    public virtual void NewList(List<string>? aList)
    {
        Items = aList;
        if (aList is not null)
            SetRange(aList.Count);
        else
            SetRange(0);
        if (Range > 0)
            FocusItem(0);
        DrawView();
    }

    /// <summary>Reads the items and the focused item from <paramref name="rec"/> (a <see cref="TListBoxRec"/>).</summary>
    public override void SetData(ReadOnlySpan<object?> rec)
    {
        var p = rec[0] is TListBoxRec r ? r : default;
        NewList(p.Items);
        FocusItem(p.Selection);
        DrawView();
    }

    /// <summary>The items, or null.</summary>
    public List<string>? List() => Items;
}
