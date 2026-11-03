using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.ColorSel.ColorSelCommands;
using static SharpVision.Events.EventCodes;

namespace SharpVision.ColorSel;

/// <summary>The items of the focused group; focusing one edits its palette entry.</summary>
/// Ported from source/tvision/colorsel.cpp, include/tvision/colorsel.h (class TColorItemList).
public class TColorItemList : TListViewer
{
    /// <summary>The first item.</summary>
    protected TColorItem? Items;

    /// <summary>Creates the list over the items starting at <paramref name="aItems"/>.</summary>
    public TColorItemList(TRect bounds, TScrollBar? aScrollBar, TColorItem? aItems)
        : base(bounds, 1, null, aScrollBar)
    {
        Items = aItems;
        EventMask |= evBroadcast;
        var i = 0;
        for (; aItems is not null; aItems = aItems.Next)
            i++;
        SetRange(i);
    }

    /// <summary>Focuses <paramref name="item"/>, saves it in the group list and broadcasts its palette index.</summary>
    public override void FocusItem(int item)
    {
        base.FocusItem(item);
        Message(Owner, evBroadcast, cmSaveColorIndex, item);

        var curItem = Items;
        while (curItem is not null && item-- > 0)
            curItem = curItem.Next;
        if (curItem is not null)
            Message(Owner, evBroadcast, cmNewColorIndex, curItem.Index);
    }

    /// <summary>The name of item <paramref name="item"/>.</summary>
    public override string GetText(int item, int maxLen)
    {
        var curItem = Items;
        while (curItem is not null && item-- > 0)
            curItem = curItem.Next;
        if (curItem is null)
            return "";
        return curItem.Name.Length > maxLen ? curItem.Name[..maxLen] : curItem.Name;
    }

    /// <summary>Shows the items of the group broadcast with <c>cmNewColorItem</c>.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evBroadcast && ev.Message.Command == cmNewColorItem)
        {
            var g = (TColorGroup)ev.Message.InfoPtr!;
            var i = 0;
            Items = g.Items;
            for (var curItem = Items; curItem is not null; curItem = curItem.Next)
                i++;
            SetRange(i);
            FocusItem(g.Index);
            DrawView();
        }
    }
}
