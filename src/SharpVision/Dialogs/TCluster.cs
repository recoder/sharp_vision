// Ported from source/tvision/tcluster.cpp, include/tvision/dialogs.h (class TCluster).

using System;
using System.Collections.Generic;

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Objects.Util;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Dialogs;

/// <summary>
/// Base class of check boxes and radio buttons: a column-wrapped list of items, each with
/// a box, a marker and a label with an optional <c>~</c> hotkey.
/// </summary>
/// <remarks>
/// Palette layout: 1 = normal text, 2 = selected text, 3 = normal shortcut, 4 = selected shortcut,
/// 5 = disabled text.
/// </remarks>
public class TCluster : TView
{
    private static readonly TPalette _palette = new([0x10, 0x11, 0x12, 0x12, 0x1f]);

    /// <summary>The cluster's value; its meaning depends on the subclass.</summary>
    protected uint Value;

    /// <summary>Bit <c>n</c> set means item <c>n</c> is enabled.</summary>
    protected uint EnableMask;

    /// <summary>The selected item.</summary>
    protected int Sel;

    /// <summary>The item labels.</summary>
    protected List<string> Strings = [];

    /// <summary>Creates a cluster with the items in <paramref name="aStrings"/>.</summary>
    public TCluster(TRect bounds, TSItem? aStrings)
        : base(bounds)
    {
        Options |= (ushort)(ofSelectable | ofFirstClick | ofPreProcess | ofPostProcess);
        for (var p = aStrings; p is not null; p = p.Next)
            Strings.Add(p.Value);

        SetCursor(2, 0);
        ShowCursor();
        EnableMask = 0xFFFFFFFF;
    }

    /// <summary>One data slot holding the value as a <see cref="ushort"/>.</summary>
    public override int DataSize() => 1;

    /// <summary>Draws the items with a one-character marker.</summary>
    public void DrawBox(string icon, char marker) => DrawMultiBox(icon, $" {marker}");

    /// <summary>
    /// Draws the items using <paramref name="icon"/> as the box and the character of
    /// <paramref name="marker"/> at <see cref="MultiMark"/> as each item's mark.
    /// </summary>
    public void DrawMultiBox(string icon, string marker)
    {
        var b = new TDrawBuffer();
        TAttrPair color;
        int i, j, cur;

        TAttrPair cNorm = GetColor(0x0301);
        TAttrPair cSel = GetColor(0x0402);
        TAttrPair cDis = GetColor(0x0505);
        // UPSTREAM: the loop also draws row Size.Y, which is outside the view and gets clipped.
        for (i = 0; i <= Size.Y; i++)
        {
            b.MoveChar(0, ' ', cNorm, Size.X);
            for (j = 0; j <= (Strings.Count - 1) / Size.Y + 1; j++)
            {
                cur = j * Size.Y + i;
                if (cur < Strings.Count)
                {
                    var col = Column(cur);
                    if (col < Size.X)
                    {
                        if (!ButtonState(cur))
                            color = cDis;
                        else if (cur == Sel && (State & sfSelected) != 0)
                            color = cSel;
                        else
                            color = cNorm;
                        b.MoveChar(col, ' ', color, Size.X - col);
                        b.MoveCStr(col, icon, color);

                        b.PutChar(col + 2, marker[MultiMark(cur)]);
                        b.MoveCStr(col + 5, Strings[cur], color);
                        if (ShowMarkers && (State & sfSelected) != 0 && cur == Sel)
                        {
                            b.PutChar(col, SpecialChars[0]);
                            b.PutChar(Column(cur + Size.Y) - 1, SpecialChars[1]);
                        }
                    }
                }
            }
            WriteBuf(0, i, Size.X, 1, b);
        }
        SetCursor(Column(Sel) + 2, Row(Sel));
    }

    /// <summary>Writes the value into <paramref name="rec"/> as a <see cref="ushort"/>.</summary>
    public override void GetData(Span<object?> rec)
    {
        rec[0] = (ushort)Value;
        DrawView();
    }

    /// <summary>The help context plus the selected item, or <c>hcNoContext</c>.</summary>
    public override ushort GetHelpCtx()
    {
        if (HelpCtx == hcNoContext)
            return hcNoContext;
        return (ushort)(HelpCtx + Sel);
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    private void MoveSel(int i, int s)
    {
        if (i <= Strings.Count)
        {
            Sel = s;
            MovedTo(Sel);
            DrawView();
        }
    }

    /// <summary>Selects and presses items with the mouse, the arrow keys, the hotkeys and Space.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if ((Options & ofSelectable) == 0)
            return;
        if (ev.What == evMouseDown)
        {
            var mouse = MakeLocal(ev.Mouse.Where);
            var i = FindSel(mouse);
            if (i != -1 && ButtonState(i))
                Sel = i;
            DrawView();
            do
            {
                mouse = MakeLocal(ev.Mouse.Where);
                if (FindSel(mouse) == Sel && ButtonState(Sel))
                    ShowCursor();
                else
                    HideCursor();
            } while (MouseEvent(ref ev, evMouseMove));
            ShowCursor();
            mouse = MakeLocal(ev.Mouse.Where);
            if (FindSel(mouse) == Sel)
            {
                Press(Sel);
                DrawView();
            }
            ClearEvent(ref ev);
        }
        else if (ev.What == evKeyDown)
        {
            var s = Sel;
            switch (CtrlToArrow(ev.KeyDown.KeyCode))
            {
                case kbUp:
                    if ((State & sfFocused) != 0)
                    {
                        var i = 0;
                        do
                        {
                            i++;
                            s--;
                            if (s < 0)
                                s = Strings.Count - 1;
                        } while (!(ButtonState(s) || i > Strings.Count));
                        MoveSel(i, s);
                        ClearEvent(ref ev);
                    }
                    break;
                case kbDown:
                    if ((State & sfFocused) != 0)
                    {
                        var i = 0;
                        do
                        {
                            i++;
                            s++;
                            if (s >= Strings.Count)
                                s = 0;
                        } while (!(ButtonState(s) || i > Strings.Count));
                        MoveSel(i, s);
                        ClearEvent(ref ev);
                    }
                    break;
                case kbRight:
                    if ((State & sfFocused) != 0)
                    {
                        var i = 0;
                        do
                        {
                            i++;
                            s += Size.Y;
                            if (s >= Strings.Count)
                                s = 0;
                        } while (!(ButtonState(s) || i > Strings.Count));
                        MoveSel(i, s);
                        ClearEvent(ref ev);
                    }
                    break;
                case kbLeft:
                    if ((State & sfFocused) != 0)
                    {
                        var i = 0;
                        do
                        {
                            i++;
                            if (s > 0)
                            {
                                s -= Size.Y;
                                if (s < 0)
                                {
                                    s = (Strings.Count + Size.Y - 1) / Size.Y * Size.Y + s - 1;
                                    if (s >= Strings.Count)
                                        s = Strings.Count - 1;
                                }
                            }
                            else
                                s = Strings.Count - 1;
                        } while (!(ButtonState(s) || i > Strings.Count));
                        MoveSel(i, s);
                        ClearEvent(ref ev);
                    }
                    break;
                default:
                    for (var i = 0; i < Strings.Count; i++)
                    {
                        var c = HotKey(Strings[i]);
                        if (ev.KeyDown.KeyCode != 0 &&
                            (GetAltCode(c) == ev.KeyDown.KeyCode ||
                             ((Owner!.Phase == PhaseType.phPostProcess || (State & sfFocused) != 0) &&
                              c != 0 &&
                              c == char.ToUpperInvariant((char)ev.KeyDown.CharScan.CharCode))))
                        {
                            if (ButtonState(i))
                            {
                                if (Focus())
                                {
                                    Sel = i;
                                    MovedTo(Sel);
                                    Press(Sel);
                                    DrawView();
                                }
                                ClearEvent(ref ev);
                            }
                            return;
                        }
                    }
                    if (ev.KeyDown.CharScan.CharCode == ' ' && (State & sfFocused) != 0)
                    {
                        Press(Sel);
                        DrawView();
                        ClearEvent(ref ev);
                    }
                    break;
            }
        }
    }

    /// <summary>Enables or disables the items in <paramref name="aMask"/>; the cluster is unselectable when all are disabled.</summary>
    public virtual void SetButtonState(uint aMask, bool enable)
    {
        if (!enable)
            EnableMask &= ~aMask;
        else
            EnableMask |= aMask;

        var n = Strings.Count;
        if (n < 32)
        {
            var testMask = (1u << n) - 1;
            if ((EnableMask & testMask) != 0)
                Options |= ofSelectable;
            else
                Options &= unchecked((ushort)~ofSelectable);
        }
    }

    /// <summary>Reads the value from <paramref name="rec"/> (a <see cref="ushort"/>).</summary>
    public override void SetData(ReadOnlySpan<object?> rec)
    {
        Value = rec[0] is ushort v ? v : 0u;
        DrawView();
    }

    /// <summary>Redraws when the cluster is selected or deselected.</summary>
    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);
        if (aState == sfSelected)
            DrawView();
    }

    /// <summary>Whether <paramref name="item"/> is marked. False by default.</summary>
    public virtual bool Mark(int item) => false;

    /// <summary>The index into the marker string for <paramref name="item"/>.</summary>
    public virtual byte MultiMark(int item) => (byte)(Mark(item) ? 1 : 0);

    /// <summary>Called when the selection moves to <paramref name="item"/>. Does nothing by default.</summary>
    public virtual void MovedTo(int item)
    {
    }

    /// <summary>Called when <paramref name="item"/> is pressed. Does nothing by default.</summary>
    public virtual void Press(int item)
    {
    }

    private int Column(int item)
    {
        if (item < Size.Y)
            return 0;
        int width = 0, col = -6, l = 0;
        for (var i = 0; i <= item; i++)
        {
            if (i % Size.Y == 0)
            {
                col += width + 6;
                width = 0;
            }

            if (i < Strings.Count)
                l = Cstrlen(Strings[i]);
            if (l > width)
                width = l;
        }
        return col;
    }

    private int FindSel(TPoint p)
    {
        var r = GetExtent();
        if (!r.Contains(p))
            return -1;
        var i = 0;
        while (p.X >= Column(i + Size.Y))
            i += Size.Y;
        var s = i + p.Y;
        return s >= Strings.Count ? -1 : s;
    }

    private int Row(int item) => item % Size.Y;

    /// <summary>Whether <paramref name="item"/> is enabled.</summary>
    public bool ButtonState(int item) => item < 32 && (EnableMask & (1u << item)) != 0;
}
