// Ported from include/tvision/views.h (class TView), source/tvision/tview.cpp,
// source/tvision/mapcolor.cpp and source/tvision/misc.cpp (message).

using System;

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Views;

/// <summary>
/// The base of every visible element: a rectangle on the screen that draws itself and
/// handles events. Views are owned by a <see cref="TGroup"/>.
/// </summary>
public partial class TView : TObject
{
    /// <summary>The event routing phase of a group (see <see cref="TGroup.Phase"/>).</summary>
    public enum PhaseType
    {
        /// <summary>Delivering to the focused view.</summary>
        phFocused,

        /// <summary>Delivering to views with <c>ofPreProcess</c>, before the focused view.</summary>
        phPreProcess,

        /// <summary>Delivering to views with <c>ofPostProcess</c>, after the focused view.</summary>
        phPostProcess,
    }

    /// <summary>How <see cref="TGroup.SetCurrent"/> changes the selection.</summary>
    public enum SelectMode
    {
        /// <summary>Deselect the old view and select the new one.</summary>
        normalSelect,

        /// <summary>Select the new view without deselecting the old one (entering a modal view).</summary>
        enterSelect,

        /// <summary>Deselect the old view without selecting the new one (leaving a modal view).</summary>
        leaveSelect,
    }

    /// <summary>Size of the shadow drawn under views with <c>sfShadow</c>.</summary>
    public static TPoint ShadowSize = new(2, 1);

    /// <summary>Attribute of shadowed cells.</summary>
    public static TColorAttr ShadowAttr = 0x08;

    /// <summary>Whether to show markers for monochrome displays.</summary>
    public static bool ShowMarkers;

    /// <summary>The attribute returned for invalid palette indexes.</summary>
    public static TColorAttr ErrorAttr = 0xCF;

    /// <summary>Whether <see cref="CurCommandSet"/> changed since the application last broadcast <c>cmCommandSetChanged</c>.</summary>
    public static bool CommandSetChanged;

    /// <summary>The enabled commands.</summary>
    public static TCommandSet CurCommandSet = InitCommands();

    // The modal view being executed by TGroup.ExecView, if any (upstream's global 'TheTopView').
    internal static TView? TheTopView;

    /// <summary>The next view in the owner's circular list of subviews.</summary>
    public TView? Next;

    /// <summary>Size in columns and rows.</summary>
    public TPoint Size;

    /// <summary>Option flags (<c>ofXxx</c>).</summary>
    public ushort Options;

    /// <summary>The events this view handles (<c>evXxx</c> masks).</summary>
    public ushort EventMask = evMouseDown | evKeyDown | evCommand;

    /// <summary>State flags (<c>sfXxx</c>).</summary>
    public ushort State = sfVisible;

    /// <summary>Position of the top-left corner, relative to the owner.</summary>
    public TPoint Origin;

    /// <summary>Cursor position, relative to the view.</summary>
    public TPoint Cursor;

    /// <summary>How the view resizes with its owner (<c>gfXxx</c>).</summary>
    public byte GrowMode;

    /// <summary>How the view can be dragged (<c>dmXxx</c>).</summary>
    public byte DragMode = (byte)dmLimitLoY;

    /// <summary>Help context (<c>hcXxx</c>).</summary>
    public ushort HelpCtx = hcNoContext;

    /// <summary>The group that owns this view, or null.</summary>
    public TGroup? Owner;

    private TPoint _resizeBalance;

    private static readonly TPalette _emptyPalette = new([]);

    /// <summary>Creates a view covering <paramref name="bounds"/>.</summary>
    public TView(TRect bounds)
    {
        SetBounds(bounds);
    }

    private static TCommandSet InitCommands()
    {
        var temp = new TCommandSet();
        for (var i = 0; i < 256; i++)
            temp.EnableCmd(i);
        temp.DisableCmd(cmZoom);
        temp.DisableCmd(cmClose);
        temp.DisableCmd(cmResize);
        temp.DisableCmd(cmNext);
        temp.DisableCmd(cmPrev);
        return temp;
    }

    /// <summary>
    /// Sends a message event to <paramref name="receiver"/>. Returns the
    /// <c>InfoPtr</c> of the event if it was handled (by default, the view that handled
    /// it), otherwise null.
    /// </summary>
    public static object? Message(TView? receiver, ushort what, ushort command, object? infoPtr)
    {
        if (receiver is null)
            return null;
        var ev = new TEvent { What = what };
        ev.Message.Command = command;
        ev.Message.InfoPtr = infoPtr;
        receiver.HandleEvent(ref ev);
        return ev.What == evNothing ? ev.Message.InfoPtr : null;
    }

    /// <summary>Called after the view is loaded and inserted. Does nothing by default.</summary>
    public virtual void Awaken()
    {
    }

    /// <summary>Makes the cursor a block (insert mode).</summary>
    public void BlockCursor() => SetState(sfCursorIns, true);

    private static int Range(int val, int min, int max)
    {
        if (min > max)
            min = max;
        if (val < min)
            return min;
        if (val > max)
            return max;
        return val;
    }

    // Fits val into the range, but keeps track of the remainders and applies them back when
    // possible. This allows views to recover their original sizes.
    private static int BalancedRange(int val, int min, int max, ref int balance)
    {
        if (min > max)
            max = min;
        if (val < min)
        {
            balance += val - min;
            return min;
        }
        if (val > max)
        {
            balance += val - max;
            return max;
        }
        var offset = Range(val + balance, min, max) - val;
        balance -= offset;
        return val + offset;
    }

    private static void FitToLimits(int a, ref int b, int min, int max, ref int balance) =>
        b = a + BalancedRange(b - a, min, max, ref balance);

    private void Grow(int s, int d, ref int i)
    {
        if ((GrowMode & gfGrowRel) != 0)
        {
            if (s != d)
                i = (i * s + ((s - d) >> 1)) / (s - d);
        }
        else
            i += d;
    }

    /// <summary>
    /// Computes the new bounds of the view after its owner's size changed by
    /// <paramref name="delta"/>, according to <see cref="GrowMode"/>.
    /// </summary>
    public virtual void CalcBounds(out TRect bounds, TPoint delta)
    {
        bounds = GetBounds();

        int s = (short)Owner!.Size.X;
        int d = (short)delta.X;

        if ((GrowMode & gfGrowLoX) != 0)
            Grow(s, d, ref bounds.A.X);
        if ((GrowMode & gfGrowHiX) != 0)
            Grow(s, d, ref bounds.B.X);

        s = (short)Owner.Size.Y;
        d = (short)delta.Y;

        if ((GrowMode & gfGrowLoY) != 0)
            Grow(s, d, ref bounds.A.Y);
        if ((GrowMode & gfGrowHiY) != 0)
            Grow(s, d, ref bounds.B.Y);

        SizeLimits(out var minLim, out var maxLim);
        FitToLimits(bounds.A.X, ref bounds.B.X, minLim.X, maxLim.X, ref _resizeBalance.X);
        FitToLimits(bounds.A.Y, ref bounds.B.Y, minLim.Y, maxLim.Y, ref _resizeBalance.Y);
    }

    /// <summary>Moves and resizes the view to <paramref name="bounds"/> and redraws it.</summary>
    public virtual void ChangeBounds(TRect bounds)
    {
        SetBounds(bounds);
        DrawView();
    }

    /// <summary>Marks <paramref name="ev"/> as handled by this view.</summary>
    public void ClearEvent(ref TEvent ev)
    {
        ev.What = evNothing;
        ev.Message.InfoPtr = this;
    }

    /// <summary>Whether <paramref name="command"/> is enabled. Commands above 255 are always enabled.</summary>
    public static bool CommandEnabled(ushort command) =>
        command > 255 || CurCommandSet.Has(command);

    /// <summary>
    /// The number of data slots the view reads and writes in <see cref="GetData"/> and
    /// <see cref="SetData"/>. 0 by default.
    /// </summary>
    public virtual int DataSize() => 0;

    /// <summary>Disables <paramref name="commands"/>.</summary>
    public static void DisableCommands(TCommandSet commands)
    {
        CommandSetChanged = CommandSetChanged || !(CurCommandSet & commands).IsEmpty();
        CurCommandSet.DisableCmd(commands);
    }

    /// <summary>Disables <paramref name="command"/>.</summary>
    public static void DisableCommand(ushort command)
    {
        CommandSetChanged = CommandSetChanged || CurCommandSet.Has(command);
        CurCommandSet.DisableCmd(command);
    }

    private void MoveGrow(TPoint p, TPoint s, TRect limits, TPoint minSize, TPoint maxSize, byte mode)
    {
        s.X = Math.Min(Math.Max(s.X, minSize.X), maxSize.X);
        s.Y = Math.Min(Math.Max(s.Y, minSize.Y), maxSize.Y);
        p.X = Math.Min(Math.Max(p.X, limits.A.X - s.X + 1), limits.B.X - 1);
        p.Y = Math.Min(Math.Max(p.Y, limits.A.Y - s.Y + 1), limits.B.Y - 1);

        if ((mode & dmLimitLoX) != 0)
            p.X = Math.Max(p.X, limits.A.X);
        if ((mode & dmLimitLoY) != 0)
            p.Y = Math.Max(p.Y, limits.A.Y);
        if ((mode & dmLimitHiX) != 0)
            p.X = Math.Min(p.X, limits.B.X - s.X);
        if ((mode & dmLimitHiY) != 0)
            p.Y = Math.Min(p.Y, limits.B.Y - s.Y);
        var r = new TRect(p.X, p.Y, p.X + s.X, p.Y + s.Y);
        Locate(ref r);
    }

    private static void Change(byte mode, TPoint delta, ref TPoint p, ref TPoint s, ushort ctrlState)
    {
        if ((mode & dmDragMove) != 0 && (ctrlState & kbShift) == 0)
            p += delta;
        else if ((mode & dmDragGrow) != 0 && (ctrlState & kbShift) != 0)
            s += delta;
    }

    /// <summary>
    /// Drags the view with the mouse (if <paramref name="ev"/> is a mouse event) or the
    /// keyboard, moving or resizing it within <paramref name="limits"/> according to
    /// <paramref name="mode"/> (<c>dmXxx</c>).
    /// </summary>
    public virtual void DragView(ref TEvent ev, byte mode, TRect limits, TPoint minSize, TPoint maxSize)
    {
        TPoint p, s;
        SetState(sfDragging, true);

        if (ev.What == evMouseDown)
        {
            if ((mode & dmDragMove) != 0)
            {
                p = Origin - ev.Mouse.Where;
                do
                {
                    ev.Mouse.Where += p;
                    MoveGrow(ev.Mouse.Where, Size, limits, minSize, maxSize, mode);
                } while (MouseEvent(ref ev, evMouseMove));
            }
            else if ((mode & dmDragGrow) != 0)
            {
                p = Size - ev.Mouse.Where;
                do
                {
                    ev.Mouse.Where += p;
                    MoveGrow(Origin, ev.Mouse.Where, limits, minSize, maxSize, mode);
                } while (MouseEvent(ref ev, evMouseMove));
            }
            else // dmDragGrowLeft
            {
                var bounds = GetBounds();
                s = Origin;
                s.Y += Size.Y;
                p = s - ev.Mouse.Where;
                do
                {
                    ev.Mouse.Where += p;
                    bounds.A.X = Math.Min(Math.Max(ev.Mouse.Where.X, bounds.B.X - maxSize.X), bounds.B.X - minSize.X);
                    bounds.B.Y = ev.Mouse.Where.Y;
                    MoveGrow(bounds.A, bounds.B - bounds.A, limits, minSize, maxSize, mode);
                } while (MouseEvent(ref ev, evMouseMove));
            }
        }
        else
        {
            var saveBounds = GetBounds();
            do
            {
                p = Origin;
                s = Size;
                KeyEvent(ref ev);
                var ctrlState = ev.KeyDown.ControlKeyState;
                switch (ev.KeyDown.KeyCode & 0xFF00)
                {
                    case kbLeft: Change(mode, new TPoint(-1, 0), ref p, ref s, ctrlState); break;
                    case kbRight: Change(mode, new TPoint(1, 0), ref p, ref s, ctrlState); break;
                    case kbUp: Change(mode, new TPoint(0, -1), ref p, ref s, ctrlState); break;
                    case kbDown: Change(mode, new TPoint(0, 1), ref p, ref s, ctrlState); break;
                    case kbCtrlLeft: Change(mode, new TPoint(-8, 0), ref p, ref s, ctrlState); break;
                    case kbCtrlRight: Change(mode, new TPoint(8, 0), ref p, ref s, ctrlState); break;
                    case kbCtrlUp: Change(mode, new TPoint(0, -4), ref p, ref s, ctrlState); break;
                    case kbCtrlDown: Change(mode, new TPoint(0, 4), ref p, ref s, ctrlState); break;
                    case kbHome: p.X = limits.A.X; break;
                    case kbEnd: p.X = limits.B.X - s.X; break;
                    case kbPgUp: p.Y = limits.A.Y; break;
                    case kbPgDn: p.Y = limits.B.Y - s.Y; break;
                }
                MoveGrow(p, s, limits, minSize, maxSize, mode);
            } while (ev.KeyDown.KeyCode != kbEsc && ev.KeyDown.KeyCode != kbEnter);
            if (ev.KeyDown.KeyCode == kbEsc)
                Locate(ref saveBounds);
        }
        SetState(sfDragging, false);
    }

    /// <summary>Draws the view. By default, fills it with spaces in color 1 of its palette.</summary>
    public virtual void Draw()
    {
        var b = new TDrawBuffer();
        b.MoveChar(0, ' ', GetColor(1), Size.X);
        WriteLine(0, 0, Size.X, Size.Y, b);
    }

    /// <summary>Updates the hardware cursor if the view is focused.</summary>
    public void DrawCursor()
    {
        if ((State & sfFocused) != 0)
            ResetCursor();
    }

    /// <summary>Redraws what the view (and its shadow) covered, after it has been hidden.</summary>
    public void DrawHide(TView? lastView)
    {
        DrawCursor();
        DrawUnderView((State & sfShadow) != 0, lastView);
    }

    /// <summary>Draws the view and its shadow, after it has been shown.</summary>
    public void DrawShow(TView? lastView)
    {
        DrawView();
        if ((State & sfShadow) != 0)
            DrawUnderView(true, lastView);
    }

    /// <summary>Redraws the views below this one (down to <paramref name="lastView"/>) within <paramref name="r"/>.</summary>
    public void DrawUnderRect(TRect r, TView? lastView)
    {
        Owner!.Clip.Intersect(r);
        Owner.DrawSubViews(NextView(), lastView);
        Owner.Clip = Owner.GetExtent();
    }

    /// <summary>Redraws the views below this one within its bounds (plus shadow and frame).</summary>
    public void DrawUnderView(bool doShadow, TView? lastView)
    {
        var r = GetBounds();
        if (doShadow)
            r.B += ShadowSize;
        if ((Options & ofFramed) != 0)
            r.Grow(1, 1);
        DrawUnderRect(r, lastView);
    }

    /// <summary>Draws the view if any part of it is exposed.</summary>
    public void DrawView()
    {
        if (Exposed())
        {
            Draw();
            DrawCursor();
        }
    }

    /// <summary>Enables <paramref name="commands"/>.</summary>
    public static void EnableCommands(TCommandSet commands)
    {
        CommandSetChanged = CommandSetChanged || (CurCommandSet & commands) != commands;
        CurCommandSet += commands;
    }

    /// <summary>Enables <paramref name="command"/>.</summary>
    public static void EnableCommand(ushort command)
    {
        CommandSetChanged = CommandSetChanged || !CurCommandSet.Has(command);
        CurCommandSet += command;
    }

    /// <summary>Ends the modal state of the top view, returning <paramref name="command"/> from its execution.</summary>
    public virtual void EndModal(ushort command) => TopView()?.EndModal(command);

    /// <summary>Whether an event is waiting. The event stays queued.</summary>
    public bool EventAvail()
    {
        var ev = new TEvent();
        GetEvent(ref ev);
        if (ev.What != evNothing)
            PutEvent(ref ev);
        return ev.What != evNothing;
    }

    /// <summary>The view's bounds in owner coordinates.</summary>
    public TRect GetBounds() => new(Origin, Origin + Size);

    /// <summary>Runs the view modally. Returns <c>cmCancel</c> by default.</summary>
    public virtual ushort Execute() => cmCancel;

    /// <summary>
    /// Selects the view, focusing its owners first. Returns false if the currently selected
    /// view refuses to release focus (see <c>ofValidate</c>).
    /// </summary>
    public bool Focus()
    {
        var result = true;

        if ((State & (sfSelected | sfModal)) == 0 && Owner is not null)
        {
            result = Owner.Focus();
            if (result)
            {
                if (Owner.Current is null ||
                    (Owner.Current.Options & ofValidate) == 0 ||
                    Owner.Current.Valid(cmReleasedFocus))
                    Select();
                else
                    return false;
            }
        }
        return result;
    }

    /// <summary>The part of the view not clipped by its owner, in view coordinates.</summary>
    public TRect GetClipRect()
    {
        var clip = GetBounds();
        if (Owner is not null)
            clip.Intersect(Owner.Clip);
        clip.Move(-Origin.X, -Origin.Y);
        return clip;
    }

    /// <summary>
    /// Maps one or two palette indexes (low byte and high byte of <paramref name="color"/>)
    /// to color attributes.
    /// </summary>
    public TAttrPair GetColor(ushort color)
    {
        TAttrPair colorPair = 0;

        if ((color & 0xFF00) != 0)
            colorPair = MapColor((byte)(color >> 8)) << 8;

        colorPair |= MapColor((byte)(color & 0xFF));

        return colorPair;
    }

    /// <summary>Returns the enabled commands.</summary>
    public static void GetCommands(out TCommandSet commands) => commands = CurCommandSet;

    /// <summary>Writes the view's data into <paramref name="rec"/> (<see cref="DataSize"/> slots). Does nothing by default.</summary>
    public virtual void GetData(Span<object?> rec)
    {
    }

    /// <summary>Gets the next event, from the owner by default.</summary>
    public virtual void GetEvent(ref TEvent ev) => Owner?.GetEvent(ref ev);

    /// <summary>The view's bounds in its own coordinates.</summary>
    public TRect GetExtent() => new(0, 0, Size.X, Size.Y);

    /// <summary>The help context: <c>hcDragging</c> while dragging, otherwise <see cref="HelpCtx"/>.</summary>
    public virtual ushort GetHelpCtx()
    {
        if ((State & sfDragging) != 0)
            return hcDragging;
        return HelpCtx;
    }

    /// <summary>The view's palette. Empty by default, so colors are passed to the owner unchanged.</summary>
    public virtual TPalette GetPalette() => _emptyPalette;

    /// <summary>Whether all the flags in <paramref name="aState"/> are set.</summary>
    public bool GetState(ushort aState) => (State & aState) == aState;

    /// <summary>Resizes the view to <paramref name="x"/> by <paramref name="y"/>.</summary>
    public void GrowTo(int x, int y)
    {
        var r = new TRect(Origin.X, Origin.Y, Origin.X + (short)x, Origin.Y + (short)y);
        Locate(ref r);
    }

    /// <summary>
    /// Handles an event. By default, a mouse click selects the view (if selectable); the click
    /// is consumed unless the view has <c>ofFirstClick</c>.
    /// </summary>
    public virtual void HandleEvent(ref TEvent ev)
    {
        if (ev.What == evMouseDown &&
            (State & (sfSelected | sfDisabled)) == 0 &&
            (Options & ofSelectable) != 0 &&
            (!Focus() || (Options & ofFirstClick) == 0))
            ClearEvent(ref ev);
    }

    /// <summary>Hides the view.</summary>
    public void Hide()
    {
        if ((State & sfVisible) != 0)
            SetState(sfVisible, false);
    }

    /// <summary>Hides the cursor.</summary>
    public void HideCursor() => SetState(sfCursorVis, false);

    /// <summary>Waits for a keyboard event and returns it in <paramref name="ev"/>.</summary>
    public void KeyEvent(ref TEvent ev)
    {
        do
        {
            GetEvent(ref ev);
        } while (ev.What != evKeyDown);
    }

    /// <summary>Stops the timer <paramref name="id"/>, through the owner by default.</summary>
    public virtual void KillTimer(object? id) => Owner?.KillTimer(id);

    /// <summary>
    /// Moves and resizes the view to <paramref name="bounds"/>, after fitting its size within
    /// <see cref="SizeLimits"/>, and redraws what changed.
    /// </summary>
    public void Locate(ref TRect bounds)
    {
        SizeLimits(out var min, out var max);
        bounds.B.X = bounds.A.X + Range(bounds.B.X - bounds.A.X, min.X, max.X);
        bounds.B.Y = bounds.A.Y + Range(bounds.B.Y - bounds.A.Y, min.Y, max.Y);
        var r = GetBounds();
        if (bounds != r)
        {
            ChangeBounds(bounds);
            if (Owner is not null && (State & sfVisible) != 0)
            {
                if ((State & sfShadow) != 0)
                {
                    r.Union(bounds);
                    r.B += ShadowSize;
                }
                DrawUnderRect(r, null);
            }
        }
    }

    /// <summary>Moves the view to the front of its owner's subviews.</summary>
    public void MakeFirst() => PutInFrontOf(Owner!.First());

    /// <summary>Converts <paramref name="source"/> from view coordinates to screen coordinates.</summary>
    public TPoint MakeGlobal(TPoint source)
    {
        var temp = source + Origin;
        TView cur = this;
        while (cur.Owner is not null)
        {
            cur = cur.Owner;
            temp += cur.Origin;
        }
        return temp;
    }

    /// <summary>Converts <paramref name="source"/> from screen coordinates to view coordinates.</summary>
    public TPoint MakeLocal(TPoint source)
    {
        var temp = source - Origin;
        TView cur = this;
        while (cur.Owner is not null)
        {
            cur = cur.Owner;
            temp -= cur.Origin;
        }
        return temp;
    }

    /// <summary>
    /// Waits for an event in <paramref name="mask"/> or a mouse release and returns it in
    /// <paramref name="ev"/>. Returns false on mouse release.
    /// </summary>
    public bool MouseEvent(ref TEvent ev, ushort mask)
    {
        do
        {
            GetEvent(ref ev);
        } while ((ev.What & (mask | evMouseUp)) == 0);

        return ev.What != evMouseUp;
    }

    /// <summary>Whether the screen position <paramref name="mouse"/> is inside the view.</summary>
    public bool MouseInView(TPoint mouse)
    {
        mouse = MakeLocal(mouse);
        return GetExtent().Contains(mouse);
    }

    /// <summary>Moves the view to (<paramref name="x"/>, <paramref name="y"/>) in owner coordinates.</summary>
    public void MoveTo(int x, int y)
    {
        var r = new TRect((short)x, (short)y, (short)x + Size.X, (short)y + Size.Y);
        Locate(ref r);
    }

    /// <summary>The next subview of the owner (one step back in z-order), or null if this is the last one.</summary>
    public TView? NextView() => this == Owner!.Last ? null : Next;

    /// <summary>Makes the cursor an underline (overwrite mode).</summary>
    public void NormalCursor() => SetState(sfCursorIns, false);

    /// <summary>The previous view in the owner's circular list.</summary>
    public TView Prev()
    {
        var res = this;
        while (res.Next != this)
            res = res.Next!;
        return res;
    }

    /// <summary>The previous subview of the owner (one step forward in z-order), or null if this is the first one.</summary>
    public TView? PrevView() => this == Owner!.First() ? null : Prev();

    /// <summary>Puts <paramref name="ev"/> back so it is returned by the next <see cref="GetEvent"/>, through the owner by default.</summary>
    public virtual void PutEvent(ref TEvent ev) => Owner?.PutEvent(ref ev);

    /// <summary>
    /// Moves the view in front of <paramref name="target"/> in its owner's z-order (to the
    /// back if <paramref name="target"/> is null), redrawing what changed.
    /// </summary>
    public void PutInFrontOf(TView? target)
    {
        if (Owner is not null && target != this && target != NextView() &&
            (target is null || target.Owner == Owner))
        {
            if ((State & sfVisible) == 0)
            {
                Owner.RemoveView(this);
                Owner.InsertView(this, target);
            }
            else
            {
                var lastView = NextView();
                var p = target;
                while (p is not null && p != this)
                    p = p.NextView();
                if (p is null)
                    lastView = target;
                State &= unchecked((ushort)~sfVisible);
                if (lastView == target)
                    DrawHide(lastView);
                Owner.RemoveView(this);
                Owner.InsertView(this, target);
                State |= sfVisible;
                if (lastView != target)
                    DrawShow(lastView);
                if ((Options & ofSelectable) != 0)
                    Owner.ResetCurrent();
            }
        }
    }

    /// <summary>Makes the view the owner's current view (or brings it to the front, with <c>ofTopSelect</c>).</summary>
    public void Select()
    {
        if ((Options & ofSelectable) != 0 && Owner is not null)
        {
            if ((Options & ofTopSelect) != 0)
                MakeFirst();
            else
                Owner.SetCurrent(this, SelectMode.normalSelect);
        }
    }

    /// <summary>Sets the view's bounds without redrawing.</summary>
    public void SetBounds(TRect bounds)
    {
        Origin = bounds.A;
        Size = bounds.B - bounds.A;
    }

    /// <summary>Enables or disables <paramref name="commands"/>.</summary>
    public static void SetCmdState(TCommandSet commands, bool enable)
    {
        if (enable)
            EnableCommands(commands);
        else
            DisableCommands(commands);
    }

    /// <summary>Replaces the enabled commands.</summary>
    public static void SetCommands(TCommandSet commands)
    {
        CommandSetChanged = CommandSetChanged || CurCommandSet != commands;
        CurCommandSet = commands;
    }

    /// <summary>Moves the cursor to (<paramref name="x"/>, <paramref name="y"/>) in view coordinates.</summary>
    public void SetCursor(int x, int y)
    {
        Cursor.X = x;
        Cursor.Y = y;
        DrawCursor();
    }

    /// <summary>Reads the view's data from <paramref name="rec"/> (<see cref="DataSize"/> slots). Does nothing by default.</summary>
    public virtual void SetData(ReadOnlySpan<object?> rec)
    {
    }

    /// <summary>Sets or clears the flags in <paramref name="aState"/> and reacts to the change.</summary>
    public virtual void SetState(ushort aState, bool enable)
    {
        if (enable)
            State |= aState;
        else
            State &= (ushort)~aState;

        if (Owner is null)
            return;

        switch (aState)
        {
            case sfVisible:
                if ((Owner.State & sfExposed) != 0)
                    SetState(sfExposed, enable);
                if (enable)
                    DrawShow(null);
                else
                    DrawHide(null);
                if ((Options & ofSelectable) != 0)
                    Owner.ResetCurrent();
                break;
            case sfCursorVis:
            case sfCursorIns:
                DrawCursor();
                break;
            case sfShadow:
                DrawUnderView(true, null);
                break;
            case sfFocused:
                ResetCursor();
                Message(Owner, evBroadcast, enable ? cmReceivedFocus : cmReleasedFocus, this);
                break;
        }
    }

    /// <summary>
    /// Starts a timer that sends <c>cmTimerExpired</c> after <paramref name="timeoutMs"/>,
    /// repeating every <paramref name="periodMs"/> if positive. Returns its id, or null.
    /// </summary>
    public virtual object? SetTimer(uint timeoutMs, int periodMs = -1) =>
        Owner?.SetTimer(timeoutMs, periodMs);

    /// <summary>Shows the view.</summary>
    public void Show()
    {
        if ((State & sfVisible) == 0)
            SetState(sfVisible, true);
    }

    /// <summary>Shows the cursor.</summary>
    public void ShowCursor() => SetState(sfCursorVis, true);

    /// <summary>The minimum and maximum size of the view. By default 0 to the owner's size (unbounded with <c>gfFixed</c>).</summary>
    public virtual void SizeLimits(out TPoint min, out TPoint max)
    {
        min = default;
        if ((GrowMode & gfFixed) == 0 && Owner is not null)
            max = Owner.Size;
        else
            max = new TPoint(int.MaxValue, int.MaxValue);
    }

    /// <summary>The modal view currently running: the one being executed, or the closest modal owner.</summary>
    public TView? TopView()
    {
        if (TheTopView is not null)
            return TheTopView;
        TView? p = this;
        while (p is not null && (p.State & sfModal) == 0)
            p = p.Owner;
        return p;
    }

    /// <summary>Whether the view is in a valid state to perform <paramref name="command"/>. True by default.</summary>
    public virtual bool Valid(ushort command) => true;

    /// <summary>Whether the view is visible and contains the mouse position of <paramref name="ev"/>.</summary>
    public bool ContainsMouse(in TEvent ev) =>
        (State & sfVisible) != 0 && MouseInView(ev.Mouse.Where);

    /// <summary>Hides the view and removes it from its owner.</summary>
    public override void ShutDown()
    {
        Hide();
        Owner?.Remove(this);
        base.ShutDown();
    }

    /// <summary>
    /// Maps palette index <paramref name="index"/> through this view's palette and its
    /// owners', returning <see cref="ErrorAttr"/> for invalid indexes.
    /// </summary>
    public virtual TColorAttr MapColor(byte index)
    {
        var p = GetPalette();
        TColorAttr color;
        if (p[0] != 0)
        {
            if (index > 0 && index <= (byte)p[0])
                color = p[index];
            else
                return ErrorAttr;
        }
        else
            color = index;
        if (color == 0)
            return ErrorAttr;
        if (Owner is not null)
            return Owner.MapColor((byte)color);
        return color;
    }
}
