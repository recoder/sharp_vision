using System;

using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.ColorSel.ColorSelCommands;
using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Events.EventCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.ColorSel;

/// <summary>A dialog that edits a copy of a palette, entry by entry, grouped by <see cref="TColorGroup"/>s.</summary>
/// Ported from source/tvision/colorsel.cpp, source/tvision/tvtext1.cpp, include/tvision/colorsel.h (class TColorDialog).
public class TColorDialog : TDialog
{
    /// <summary>The dialog title.</summary>
    public static string Colors = "Colors";
    /// <summary>The group list label.</summary>
    public static string GroupText = "~G~roup";
    /// <summary>The item list label.</summary>
    public static string ItemText = "~I~tem";
    /// <summary>The foreground selector label.</summary>
    public static string ForText = "~F~oreground";
    /// <summary>The background selector label.</summary>
    public static string BakText = "~B~ackground";
    /// <summary>The sample text.</summary>
    public static string TextText = "Text ";
    /// <summary>The monochrome selector label.</summary>
    public static string ColorText = "Color";
    /// <summary>The OK button title.</summary>
    public static string OkText = "O~K~";
    /// <summary>The Cancel button title.</summary>
    public static string CancelText = "Cancel";

    private static TColorIndex? _colorIndexes;

    /// <summary>The palette being edited, a copy of the one passed in.</summary>
    public TPalette? Pal;

    /// <summary>The sample text.</summary>
    protected TColorDisplay Display;
    /// <summary>The group list.</summary>
    protected TColorGroupList Groups;
    /// <summary>The foreground selector label.</summary>
    protected TLabel ForLabel;
    /// <summary>The foreground selector.</summary>
    protected TColorSelector ForSel;
    /// <summary>The background selector label.</summary>
    protected TLabel BakLabel;
    /// <summary>The background selector.</summary>
    protected TColorSelector BakSel;
    /// <summary>The monochrome selector label.</summary>
    protected TLabel MonoLabel;
    /// <summary>The monochrome selector, shown instead of the color selectors when <see cref="TView.ShowMarkers"/> is set.</summary>
    protected TMonoSelector MonoSel;
    /// <summary>The focused group.</summary>
    protected byte GroupIndex;

    /// <summary>Creates the dialog over <paramref name="aGroups"/>, editing a copy of <paramref name="aPalette"/>.</summary>
    public TColorDialog(TPalette? aPalette, TColorGroup aGroups) : base(new TRect(0, 0, 61, 18), Colors)
    {
        Options |= ofCentered;
        if (aPalette is not null)
            Pal = new TPalette(aPalette);

        var sb = new TScrollBar(new TRect(18, 3, 19, 14));
        Insert(sb);

        Groups = new TColorGroupList(new TRect(3, 3, 18, 14), sb, aGroups);
        Insert(Groups);
        Insert(new TLabel(new TRect(2, 2, 8, 3), GroupText, Groups));

        sb = new TScrollBar(new TRect(41, 3, 42, 14));
        Insert(sb);

        var p = new TColorItemList(new TRect(21, 3, 41, 14), sb, aGroups.Items);
        Insert(p);
        Insert(new TLabel(new TRect(20, 2, 25, 3), ItemText, p));

        ForSel = new TColorSelector(new TRect(45, 3, 57, 7), TColorSelector.ColorSel.csForeground);
        Insert(ForSel);
        ForLabel = new TLabel(new TRect(45, 2, 57, 3), ForText, ForSel);
        Insert(ForLabel);

        BakSel = new TColorSelector(new TRect(45, 9, 57, 11), TColorSelector.ColorSel.csBackground);
        Insert(BakSel);
        BakLabel = new TLabel(new TRect(45, 8, 57, 9), BakText, BakSel);
        Insert(BakLabel);

        Display = new TColorDisplay(new TRect(44, 12, 58, 14), TextText);
        Insert(Display);

        MonoSel = new TMonoSelector(new TRect(44, 3, 59, 7));
        MonoSel.Hide();
        Insert(MonoSel);
        MonoLabel = new TLabel(new TRect(43, 2, 49, 3), ColorText, MonoSel);
        MonoLabel.Hide();
        Insert(MonoLabel);

        Insert(new TButton(new TRect(36, 15, 46, 17), OkText, cmOK, bfDefault));
        Insert(new TButton(new TRect(48, 15, 58, 17), CancelText, cmCancel, bfNormal));
        SelectNext(false);

        if (Pal is not null)
            SetData([Pal]);
    }

    /// <summary>Tracks the focused group and points the sample at the focused item's entry.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        if (ev.What == evBroadcast && ev.Message.Command == cmNewColorItem)
            GroupIndex = (byte)Groups.Focused;
        base.HandleEvent(ref ev);
        if (ev.What == evBroadcast && ev.Message.Command == cmNewColorIndex)
            Display.SetColor(Pal!.Data, ev.Message.InfoByte);
    }

    /// <summary>One slot: the palette.</summary>
    public override int DataSize() => 1;

    /// <summary>Copies the edited palette into the <see cref="TPalette"/> in <c>rec[0]</c>, or stores a copy there.</summary>
    public override void GetData(Span<object?> rec)
    {
        GetIndexes(ref _colorIndexes);
        if (rec[0] is TPalette p)
            p.Assign(Pal!);
        else
            rec[0] = new TPalette(Pal!);
    }

    /// <summary>Edits a copy of the <see cref="TPalette"/> in <c>rec[0]</c>.</summary>
    public override void SetData(ReadOnlySpan<object?> rec)
    {
        var src = (TPalette)rec[0]!;
        if (Pal is null)
            Pal = new TPalette(src);
        else
            Pal.Assign(src);

        SetIndexes(ref _colorIndexes);
        // UPSTREAM: indexes the palette with the group's item number; FocusItem below corrects it.
        Display.SetColor(Pal.Data, Groups.GetGroupIndex(GroupIndex));
        Groups.FocusItem(GroupIndex);
        if (ShowMarkers)
        {
            ForLabel.Hide();
            ForSel.Hide();
            BakLabel.Hide();
            BakSel.Hide();
            MonoLabel.Show();
            MonoSel.Show();
        }
        Groups.Select();
    }

    /// <summary>Restores each group's focused item and the focused group from <paramref name="colIdx"/>, resetting it if the group count changed.</summary>
    public void SetIndexes(ref TColorIndex? colIdx)
    {
        var numGroups = Groups.GetNumGroups();
        if (colIdx is not null && colIdx.ColorSize != numGroups)
            colIdx = null;
        colIdx ??= new TColorIndex { GroupIndex = 0, ColorSize = numGroups };
        for (byte index = 0; index < numGroups; index++)
            Groups.SetGroupIndex(index, colIdx.ColorIndex[index]);

        GroupIndex = colIdx.GroupIndex;
    }

    /// <summary>Saves each group's focused item and the focused group into <paramref name="colIdx"/>.</summary>
    public void GetIndexes(ref TColorIndex? colIdx)
    {
        var n = Groups.GetNumGroups();
        colIdx ??= new TColorIndex { ColorSize = n };
        colIdx.GroupIndex = GroupIndex;
        for (byte index = 0; index < n; index++)
            colIdx.ColorIndex[index] = Groups.GetGroupIndex(index);
    }
}
