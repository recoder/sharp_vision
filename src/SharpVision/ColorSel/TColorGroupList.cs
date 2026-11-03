using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.ColorSel.ColorSelCommands;
using static SharpVision.Events.EventCodes;

namespace SharpVision.ColorSel;

/// <summary>The list of color groups; focusing one shows its items in the item list.</summary>
/// Ported from source/tvision/colorsel.cpp, include/tvision/colorsel.h (class TColorGroupList).
public class TColorGroupList : TListViewer
{
    /// <summary>The first group.</summary>
    protected TColorGroup? Groups;

    /// <summary>Creates the list over the groups starting at <paramref name="aGroups"/>.</summary>
    public TColorGroupList(TRect bounds, TScrollBar? aScrollBar, TColorGroup? aGroups)
        : base(bounds, 1, null, aScrollBar)
    {
        Groups = aGroups;
        var i = 0;
        for (; aGroups is not null; aGroups = aGroups.Next)
            i++;
        SetRange(i);
    }

    /// <summary>Focuses <paramref name="item"/> and broadcasts <c>cmNewColorItem</c> with its group.</summary>
    public override void FocusItem(int item)
    {
        base.FocusItem(item);
        var curGroup = Groups;
        while (curGroup is not null && item-- > 0)
            curGroup = curGroup.Next;
        if (curGroup is not null)
            Message(Owner, evBroadcast, cmNewColorItem, curGroup);
    }

    /// <summary>The name of group <paramref name="item"/>.</summary>
    public override string GetText(int item, int maxLen)
    {
        var curGroup = Groups;
        while (curGroup is not null && item-- > 0)
            curGroup = curGroup.Next;
        if (curGroup is null)
            return "";
        return curGroup.Name.Length > maxLen ? curGroup.Name[..maxLen] : curGroup.Name;
    }

    /// <summary>Remembers the focused item of the focused group on <c>cmSaveColorIndex</c>.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evBroadcast && ev.Message.Command == cmSaveColorIndex)
            SetGroupIndex((byte)Focused, ev.Message.InfoByte);
    }

    /// <summary>Sets the focused item of group <paramref name="groupNum"/>, clamped to its last item.</summary>
    public void SetGroupIndex(byte groupNum, byte itemNum)
    {
        var g = GetGroup(groupNum);
        if (g is not null)
        {
            byte index = 0;
            var cur = g.Items;
            if (cur is not null)
                while (index < itemNum && cur.Next is { } next)
                {
                    cur = next;
                    ++index;
                }
            g.Index = index;
        }
    }

    /// <summary>The focused item of group <paramref name="groupNum"/>, or 0.</summary>
    public byte GetGroupIndex(byte groupNum) => GetGroup(groupNum)?.Index ?? 0;

    /// <summary>Group <paramref name="groupNum"/>, or null.</summary>
    public TColorGroup? GetGroup(byte groupNum)
    {
        var g = Groups;
        while (g is not null && groupNum-- > 0)
            g = g.Next;
        return g;
    }

    /// <summary>The number of groups.</summary>
    public byte GetNumGroups()
    {
        byte n = 0;
        for (var g = Groups; g is not null; g = g.Next)
            n++;
        return n;
    }
}
