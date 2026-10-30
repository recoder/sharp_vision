using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Objects.Util;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Views;

/// <summary>
/// Base class of list views: shows <see cref="Range"/> items from <see cref="GetText"/> in one
/// or more columns, with a focused item, optional scroll bars and mouse and keyboard navigation.
/// </summary>
/// <remarks>
/// Palette layout: 1 = active, 2 = inactive, 3 = focused, 4 = selected, 5 = divider.
/// Item indexes are <see cref="int"/> where upstream uses <c>short</c>.
/// </remarks>
/// Ported from source/tvision/tlstview.cpp, source/tvision/tvtext2.cpp (emptyText),
/// include/tvision/views.h (class TListViewer).
public class TListViewer : TView
{
    private static readonly TPalette _palette = new([0x1A, 0x1A, 0x1B, 0x1C, 0x1D]);

    /// <summary>The text shown when the list has no items.</summary>
    public static string EmptyText = "<empty>";

    /// <summary>The horizontal scroll bar, or null. Its value is the text indent.</summary>
    public TScrollBar? HScrollBar;

    /// <summary>The vertical scroll bar, or null. Its value follows the focused item.</summary>
    public TScrollBar? VScrollBar;

    /// <summary>The number of columns.</summary>
    public int NumCols;

    /// <summary>The first item shown.</summary>
    public int TopItem;

    /// <summary>The focused item.</summary>
    public int Focused;

    /// <summary>The number of items.</summary>
    public int Range;

    /// <summary>Creates a list viewer with <paramref name="aNumCols"/> columns and optional scroll bars.</summary>
    public TListViewer(TRect bounds, ushort aNumCols, TScrollBar? aHScrollBar, TScrollBar? aVScrollBar) : base(bounds)
    {
        int arStep, pgStep;

        NumCols = aNumCols;
        Options |= (ushort)(ofFirstClick | ofSelectable);
        EventMask |= evBroadcast;
        if (aVScrollBar is not null)
        {
            if (NumCols == 1)
            {
                pgStep = Size.Y - 1;
                arStep = 1;
            }
            else
            {
                pgStep = Size.Y * NumCols;
                arStep = Size.Y;
            }
            aVScrollBar.SetStep(pgStep, arStep);
        }

        aHScrollBar?.SetStep(Size.X / NumCols, 1);

        HScrollBar = aHScrollBar;
        VScrollBar = aVScrollBar;
    }

    /// <summary>Changes the bounds and updates the scroll bars' page steps.</summary>
    public override void ChangeBounds(TRect bounds)
    {
        base.ChangeBounds(bounds);
        HScrollBar?.SetStep(Size.X / NumCols, HScrollBar.ArStep);
        VScrollBar?.SetStep(Size.Y, VScrollBar.ArStep);
    }

    /// <summary>Draws the visible items column by column, with dividers.</summary>
    public override void Draw()
    {
        int i, j, item;
        TColorAttr normalColor, selectedColor, focusedColor, color;
        int colWidth, curCol, indent;
        var b = new TDrawBuffer();
        int scOff;
        bool focusedVis;

        if ((State & (sfSelected | sfActive)) == (sfSelected | sfActive))
        {
            normalColor = GetColor(1);
            focusedColor = GetColor(3);
            selectedColor = GetColor(4);
        }
        else
        {
            normalColor = GetColor(2);
            selectedColor = GetColor(4);
            focusedColor = 0; // Unused, but silence warning.
        }

        indent = HScrollBar?.Value ?? 0;

        focusedVis = false;
        colWidth = Size.X / NumCols + 1;
        for (i = 0; i < Size.Y; i++)
        {
            for (j = 0; j < NumCols; j++)
            {
                item = j * Size.Y + i + TopItem;
                curCol = j * colWidth;
                if ((State & (sfSelected | sfActive)) == (sfSelected | sfActive) &&
                    Focused == item &&
                    Range > 0)
                {
                    color = focusedColor;
                    SetCursor(curCol + 1, i);
                    scOff = 0;
                    focusedVis = true;
                }
                else if (item < Range && IsSelected(item))
                {
                    color = selectedColor;
                    scOff = 2;
                }
                else
                {
                    color = normalColor;
                    scOff = 4;
                }

                b.MoveChar(curCol, ' ', color, colWidth);
                if (item < Range)
                {
                    if (indent < 255)
                        b.MoveStr(curCol + 1, GetText(item, 255), color, colWidth, indent);
                    if (ShowMarkers)
                    {
                        b.PutChar(curCol, SpecialChars[scOff]);
                        b.PutChar(curCol + colWidth - 2, SpecialChars[scOff + 1]);
                    }
                }
                else if (i == 0 && j == 0)
                    b.MoveStr(curCol + 1, EmptyText, GetColor(1));

                b.MoveChar(curCol + colWidth - 1, '│', GetColor(5), 1);
            }
            WriteLine(0, i, Size.X, 1, b);
        }

        if (!focusedVis)
            SetCursor(-1, -1);
    }

    /// <summary>Focuses <paramref name="item"/> and scrolls it into view.</summary>
    public virtual void FocusItem(int item)
    {
        Focused = item;
        if (VScrollBar is not null)
            VScrollBar.SetValue(item);
        else
            DrawView();
        if (Size.Y > 0)
        {
            if (item < TopItem)
            {
                if (NumCols == 1)
                    TopItem = item;
                else
                    TopItem = item - item % Size.Y;
            }
            else if (item >= TopItem + Size.Y * NumCols)
            {
                if (NumCols == 1)
                    TopItem = item - Size.Y + 1;
                else
                    TopItem = item - item % Size.Y - Size.Y * (NumCols - 1);
            }
        }
    }

    /// <summary>Focuses <paramref name="item"/> clamped to the range; does nothing when the list is empty.</summary>
    public virtual void FocusItemNum(int item)
    {
        if (item < 0)
            item = 0;
        else if (item >= Range && Range > 0)
            item = Range - 1;

        if (Range != 0)
            FocusItem(item);
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    /// <summary>The text of <paramref name="item"/>, at most <paramref name="maxLen"/> long. Empty by default.</summary>
    public virtual string GetText(int item, int maxLen) => "";

    /// <summary>Whether <paramref name="item"/> is selected. By default, the focused item is.</summary>
    public virtual bool IsSelected(int item) => item == Focused;

    /// <summary>Moves the focus with the mouse and keys, and follows the scroll bars.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        TPoint mouse;
        int colWidth;
        int oldItem, newItem = 0;
        int count;
        const int mouseAutosToSkip = 4;

        base.HandleEvent(ref ev);

        if (ev.What == evMouseDown)
        {
            colWidth = Size.X / NumCols + 1;
            oldItem = Focused;
            count = 0;
            do
            {
                mouse = MakeLocal(ev.Mouse.Where);
                if (MouseInView(ev.Mouse.Where))
                    newItem = mouse.Y + Size.Y * (mouse.X / colWidth) + TopItem;
                else
                {
                    if (NumCols == 1)
                    {
                        if (ev.What == evMouseAuto)
                            count++;
                        if (count == mouseAutosToSkip)
                        {
                            count = 0;
                            if (mouse.Y < 0)
                                newItem = Focused - 1;
                            else if (mouse.Y >= Size.Y)
                                newItem = Focused + 1;
                        }
                    }
                    else
                    {
                        if (ev.What == evMouseAuto)
                            count++;
                        if (count == mouseAutosToSkip)
                        {
                            count = 0;
                            if (mouse.X < 0)
                                newItem = Focused - Size.Y;
                            else if (mouse.X >= Size.X)
                                newItem = Focused + Size.Y;
                            else if (mouse.Y < 0)
                                newItem = Focused - Focused % Size.Y;
                            else if (mouse.Y > Size.Y)
                                newItem = Focused - Focused % Size.Y + Size.Y - 1;
                        }
                    }
                }
                if (newItem != oldItem)
                {
                    FocusItemNum(newItem);
                    DrawView();
                }
                oldItem = newItem;
                if ((ev.Mouse.EventFlags & meDoubleClick) != 0)
                    break;
            } while (MouseEvent(ref ev, evMouseMove | evMouseAuto));
            FocusItemNum(newItem);
            DrawView();
            if ((ev.Mouse.EventFlags & meDoubleClick) != 0 && Range > newItem)
                SelectItem(newItem);
            ClearEvent(ref ev);
        }
        else if (ev.What == evKeyDown)
        {
            if (ev.KeyDown.CharScan.CharCode == ' ' && Focused < Range)
            {
                SelectItem(Focused);
                newItem = Focused;
            }
            else
            {
                switch (CtrlToArrow(ev.KeyDown.KeyCode))
                {
                    case kbUp:
                        newItem = Focused - 1;
                        break;
                    case kbDown:
                        newItem = Focused + 1;
                        break;
                    case kbRight:
                        if (NumCols > 1)
                            newItem = Focused + Size.Y;
                        else
                            return;
                        break;
                    case kbLeft:
                        if (NumCols > 1)
                            newItem = Focused - Size.Y;
                        else
                            return;
                        break;
                    case kbPgDn:
                        newItem = Focused + Size.Y * NumCols;
                        break;
                    case kbPgUp:
                        newItem = Focused - Size.Y * NumCols;
                        break;
                    case kbHome:
                        newItem = TopItem;
                        break;
                    case kbEnd:
                        newItem = TopItem + Size.Y * NumCols - 1;
                        break;
                    case kbCtrlPgDn:
                        newItem = Range - 1;
                        break;
                    case kbCtrlPgUp:
                        newItem = 0;
                        break;
                    default:
                        return;
                }
            }
            FocusItemNum(newItem);
            DrawView();
            ClearEvent(ref ev);
        }
        else if (ev.What == evBroadcast)
        {
            if ((Options & ofSelectable) != 0)
            {
                if (ev.Message.Command == cmScrollBarClicked &&
                    (ev.Message.InfoPtr == HScrollBar || ev.Message.InfoPtr == VScrollBar))
                    Select();
                else if (ev.Message.Command == cmScrollBarChanged)
                {
                    if (VScrollBar == ev.Message.InfoPtr)
                    {
                        FocusItemNum(VScrollBar!.Value);
                        DrawView();
                    }
                    else if (HScrollBar == ev.Message.InfoPtr)
                        DrawView();
                }
            }
        }
    }

    /// <summary>Broadcasts <c>cmListItemSelected</c> to the owner.</summary>
    public virtual void SelectItem(int item) => Message(Owner, evBroadcast, cmListItemSelected, this);

    /// <summary>Sets the number of items, refocusing item 0 if the focused one is gone.</summary>
    public void SetRange(int aRange)
    {
        Range = aRange;
        if (Focused >= aRange)
            Focused = 0;
        // UPSTREAM: with a scroll bar, the list itself is redrawn only if the bar's value changes.
        if (VScrollBar is not null)
            VScrollBar.SetParams(Focused, 0, aRange - 1, VScrollBar.PgStep, VScrollBar.ArStep);
        else
            DrawView();
    }

    /// <summary>Shows the scroll bars only while the list is active and visible.</summary>
    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);
        if ((aState & (sfSelected | sfActive | sfVisible)) != 0)
        {
            if (HScrollBar is not null)
            {
                if (GetState(sfActive) && GetState(sfVisible))
                    HScrollBar.Show();
                else
                    HScrollBar.Hide();
            }
            if (VScrollBar is not null)
            {
                if (GetState(sfActive) && GetState(sfVisible))
                    VScrollBar.Show();
                else
                    VScrollBar.Hide();
            }
            DrawView();
        }
    }

    /// <inheritdoc />
    public override void ShutDown()
    {
        HScrollBar = null;
        VScrollBar = null;
        base.ShutDown();
    }
}
