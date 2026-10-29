// Ported from include/tvision/views.h (class TGroup), source/tvision/tgroup.cpp,
// source/tvision/grp.cpp and source/tvision/tgrmv.cpp.

using System;

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Views;

/// <summary>
/// A view that owns other views (subviews), draws them, and routes events to them.
/// </summary>
/// <remarks>
/// Subviews form a circular list in z-order: <see cref="Last"/> is the backmost view and
/// <c>Last.Next</c> (<see cref="First"/>) the frontmost one.
/// </remarks>
public class TGroup : TView
{
    /// <summary>The selected subview, or null.</summary>
    public TView? Current;

    /// <summary>The backmost subview, or null if the group is empty.</summary>
    public TView? Last;

    /// <summary>The area subviews may draw to, in group coordinates.</summary>
    public TRect Clip;

    /// <summary>The current phase of event routing.</summary>
    public PhaseType Phase = PhaseType.phFocused;

    /// <summary>The cache of the group's appearance (with <c>ofBuffered</c>), or null.</summary>
    public TScreenCell[]? Buffer;

    /// <summary>While non-zero, drawing goes to <see cref="Buffer"/> only and is shown when the count returns to zero.</summary>
    public byte LockFlag;

    /// <summary>The command that ended the modal state (see <see cref="Execute"/>).</summary>
    public ushort EndState;

    /// <summary>Creates an empty group covering <paramref name="bounds"/>.</summary>
    public TGroup(TRect bounds)
        : base(bounds)
    {
        Options |= ofSelectable | ofBuffered;
        Clip = GetExtent();
        EventMask = 0xFFFF;
    }

    /// <summary>Hides and destroys every subview, then shuts down the group itself.</summary>
    public override void ShutDown()
    {
        var p = Last;
        if (p is not null)
        {
            do
            {
                p.Hide();
                p = p.Prev();
            } while (p != Last);

            do
            {
                var t = p!.Prev();
                Destroy(p);
                p = t;
            } while (Last is not null);
        }
        FreeBuffer();
        Current = null;
        base.ShutDown();
    }

    /// <summary>Calls <see cref="TView.Awaken"/> on every subview.</summary>
    public override void Awaken() => ForEach(v => v.Awaken());

    /// <summary>Moves and resizes the group, letting each subview adjust its bounds (see <see cref="TView.GrowMode"/>).</summary>
    public override void ChangeBounds(TRect bounds)
    {
        var d = new TPoint(
            (bounds.B.X - bounds.A.X) - Size.X,
            (bounds.B.Y - bounds.A.Y) - Size.Y);
        if (d.X == 0 && d.Y == 0)
        {
            SetBounds(bounds);
            DrawView();
        }
        else
        {
            SetBounds(bounds);
            Clip = GetExtent();
            GetBuffer();
            Lock();
            ForEach(p =>
            {
                p.CalcBounds(out var r, d);
                p.ChangeBounds(r);
            });
            Unlock();
        }
    }

    /// <summary>The total number of data slots of the subviews.</summary>
    public override int DataSize()
    {
        var t = 0;
        ForEach(p => t += p.DataSize());
        return t;
    }

    /// <summary>Removes <paramref name="p"/> from the group, keeping it visible if it was.</summary>
    public void Remove(TView? p)
    {
        if (p is not null)
        {
            var saveState = p.State;
            p.Hide();
            RemoveView(p);
            p.Owner = null;
            p.Next = null;
            if ((saveState & sfVisible) != 0)
                p.Show();
        }
    }

    /// <summary>Draws the group: from <see cref="Buffer"/> if it has one, otherwise by redrawing each subview.</summary>
    public override void Draw()
    {
        if (Buffer is null)
        {
            GetBuffer();
            if (Buffer is not null)
            {
                LockFlag++;
                Redraw();
                LockFlag--;
            }
        }
        if (Buffer is not null)
            WriteBuf(0, 0, Size.X, Size.Y, Buffer);
        else
        {
            Clip = GetClipRect();
            Redraw();
            Clip = GetExtent();
        }
    }

    /// <summary>Draws the subviews from <paramref name="p"/> up to, not including, <paramref name="bottom"/>.</summary>
    public void DrawSubViews(TView? p, TView? bottom)
    {
        while (p != bottom)
        {
            p!.DrawView();
            p = p.NextView();
        }
    }

    /// <summary>Ends the group's modal state with <paramref name="command"/>, or passes the request on if it is not modal.</summary>
    public override void EndModal(ushort command)
    {
        if ((State & sfModal) != 0)
            EndState = command;
        else
            base.EndModal(command);
    }

    /// <summary>Called for events nobody handled. Passes them to the owner by default.</summary>
    public virtual void EventError(ref TEvent ev) => Owner?.EventError(ref ev);

    /// <summary>
    /// Runs the group's event loop until <see cref="TView.EndModal"/> sets an
    /// <see cref="EndState"/> that <see cref="Valid"/> accepts, and returns it.
    /// </summary>
    public override ushort Execute()
    {
        do
        {
            EndState = 0;
            do
            {
                var e = new TEvent();
                GetEvent(ref e);
                HandleEvent(ref e);
                if (e.What != evNothing)
                    EventError(ref e);
            } while (EndState == 0);
        } while (!Valid(EndState));
        return EndState;
    }

    /// <summary>
    /// Runs <paramref name="p"/> modally (inserting it first if it has no owner) and returns
    /// the command that ended it. The command set is restored afterwards.
    /// </summary>
    public ushort ExecView(TView? p)
    {
        if (p is null)
            return cmCancel;

        var saveOptions = p.Options;
        var saveOwner = p.Owner;
        var saveTopView = TheTopView;
        var saveCurrent = Current;
        GetCommands(out var saveCommands);
        TheTopView = p;
        p.Options = (ushort)(p.Options & ~ofSelectable);
        p.SetState(sfModal, true);
        SetCurrent(p, SelectMode.enterSelect);
        if (saveOwner is null)
            Insert(p);
        var retval = p.Execute();
        if (saveOwner is null)
            Remove(p);
        SetCurrent(saveCurrent, SelectMode.leaveSelect);
        p.SetState(sfModal, false);
        p.Options = saveOptions;
        TheTopView = saveTopView;
        SetCommands(saveCommands);
        return retval;
    }

    /// <summary>The frontmost subview, or null if the group is empty.</summary>
    public TView? First() => Last?.Next;

    private TView? FindNext(bool forwards)
    {
        TView? result = null;
        if (Current is not null)
        {
            var p = Current;
            do
            {
                p = forwards ? p.Next! : p.Prev();
            } while (!(((p.State & (sfVisible | sfDisabled)) == sfVisible &&
                        (p.Options & ofSelectable) != 0) || p == Current));

            if (p != Current)
                result = p;
        }
        return result;
    }

    /// <summary>Focuses the next (or previous) selectable subview. Returns false if focus could not move.</summary>
    public bool FocusNext(bool forwards)
    {
        var p = FindNext(forwards);
        return p is null || p.Focus();
    }

    /// <summary>The first subview, starting from the back, with all of <paramref name="aState"/> and <paramref name="aOptions"/> set.</summary>
    public TView? FirstMatch(ushort aState, ushort aOptions)
    {
        if (Last is null)
            return null;

        var temp = Last;
        while (true)
        {
            if ((temp.State & aState) == aState && (temp.Options & aOptions) == aOptions)
                return temp;

            temp = temp.Next!;
            if (temp == Last)
                return null;
        }
    }

    /// <summary>Discards <see cref="Buffer"/>.</summary>
    public void FreeBuffer()
    {
        if ((Options & ofBuffered) != 0 && Buffer is not null)
            Buffer = null;
    }

    /// <summary>Allocates an empty <see cref="Buffer"/>, if the group is exposed and has <c>ofBuffered</c>.</summary>
    public void GetBuffer()
    {
        if ((State & sfExposed) != 0 && (Options & ofBuffered) != 0)
            Buffer = new TScreenCell[Math.Max(Size.X * Size.Y, 0)];
    }

    /// <summary>Collects the data of the subviews, from the back, into consecutive slots of <paramref name="rec"/>.</summary>
    public override void GetData(Span<object?> rec)
    {
        var i = 0;
        if (Last is not null)
        {
            var v = Last;
            do
            {
                v.GetData(rec[i..]);
                i += v.DataSize();
                v = v.Prev();
            } while (v != Last);
        }
    }

    private void DoHandleEvent(TView? p, ref TEvent ev)
    {
        if (p is null ||
            ((p.State & sfDisabled) != 0 && (ev.What & (positionalEvents | focusedEvents)) != 0))
            return;

        switch (Phase)
        {
            case PhaseType.phFocused:
                break;
            case PhaseType.phPreProcess:
                if ((p.Options & ofPreProcess) == 0)
                    return;
                break;
            case PhaseType.phPostProcess:
                if ((p.Options & ofPostProcess) == 0)
                    return;
                break;
        }
        if ((ev.What & p.EventMask) != 0)
            p.HandleEvent(ref ev);
    }

    // forEach with the event passed by reference, which a lambda cannot capture.
    private void ForEachHandleEvent(ref TEvent ev)
    {
        var term = Last;
        var temp = Last;
        if (temp is null)
            return;
        var next = temp.Next!;
        do
        {
            temp = next;
            next = temp.Next!;
            DoHandleEvent(temp, ref ev);
        } while (temp != term);
    }

    /// <summary>
    /// Routes <paramref name="ev"/> to the subviews. Focused events (keyboard and commands)
    /// go to <c>ofPreProcess</c> views, the current view, then <c>ofPostProcess</c> views.
    /// Positional (mouse) events go to the frontmost view under the mouse. Other events go
    /// to every subview.
    /// </summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if ((ev.What & focusedEvents) != 0)
        {
            Phase = PhaseType.phPreProcess;
            ForEachHandleEvent(ref ev);

            Phase = PhaseType.phFocused;
            DoHandleEvent(Current, ref ev);

            Phase = PhaseType.phPostProcess;
            ForEachHandleEvent(ref ev);
        }
        else if (ev.What != 0)
        {
            Phase = PhaseType.phFocused;
            if ((ev.What & positionalEvents) != 0)
            {
                var mouseEvent = ev;
                DoHandleEvent(FirstThat(p => p.ContainsMouse(mouseEvent)), ref ev);
            }
            else
                ForEachHandleEvent(ref ev);
        }
    }

    /// <summary>Inserts <paramref name="p"/> in front of all the other subviews.</summary>
    public void Insert(TView? p) => InsertBefore(p, First());

    /// <summary>
    /// Inserts <paramref name="p"/> (which must not have an owner) in front of
    /// <paramref name="target"/>, or at the back if <paramref name="target"/> is null,
    /// centering it if it has <c>ofCenterX</c>/<c>ofCenterY</c>.
    /// </summary>
    public void InsertBefore(TView? p, TView? target)
    {
        if (p is not null && p.Owner is null && (target is null || target.Owner == this))
        {
            if ((p.Options & ofCenterX) != 0)
                p.Origin.X = (Size.X - p.Size.X) / 2;
            if ((p.Options & ofCenterY) != 0)
                p.Origin.Y = (Size.Y - p.Size.Y) / 2;
            var saveState = p.State;
            p.Hide();
            InsertView(p, target);
            if ((saveState & sfVisible) != 0)
                p.Show();
            if ((saveState & sfActive) != 0)
                p.SetState(sfActive, true);
        }
    }

    /// <summary>Links <paramref name="p"/> into the list in front of <paramref name="target"/> (at the back if null), without drawing.</summary>
    public void InsertView(TView p, TView? target)
    {
        p.Owner = this;
        if (target is not null)
        {
            target = target.Prev();
            p.Next = target.Next;
            target.Next = p;
        }
        else
        {
            if (Last is null)
                p.Next = p;
            else
            {
                p.Next = Last.Next;
                Last.Next = p;
            }
            Last = p;
        }
    }

    /// <summary>Defers drawing to the screen until the matching <see cref="Unlock"/>, if the group has a buffer.</summary>
    public void Lock()
    {
        if (Buffer is not null || LockFlag != 0)
            LockFlag++;
    }

    /// <summary>Draws every subview.</summary>
    public void Redraw() => DrawSubViews(First(), null);

    /// <summary>Selects the frontmost visible, selectable subview.</summary>
    public void ResetCurrent() => SetCurrent(FirstMatch(sfVisible, ofSelectable), SelectMode.normalSelect);

    /// <summary>Updates the caret for the current subview.</summary>
    public override void ResetCursor() => Current?.ResetCursor();

    /// <summary>Selects the next (or previous) selectable subview.</summary>
    public void SelectNext(bool forwards)
    {
        if (Current is not null)
            FindNext(forwards)?.Select();
    }

    private static void SelectView(TView? p, bool enable) => p?.SetState(sfSelected, enable);

    private void FocusView(TView? p, bool enable)
    {
        if ((State & sfFocused) != 0 && p is not null)
            p.SetState(sfFocused, enable);
    }

    /// <summary>Makes <paramref name="p"/> the current subview, moving selection and focus according to <paramref name="mode"/>.</summary>
    public void SetCurrent(TView? p, SelectMode mode)
    {
        if (Current != p)
        {
            Lock();
            FocusView(Current, false);
            if (mode != SelectMode.enterSelect)
                SelectView(Current, false);
            if (mode != SelectMode.leaveSelect)
                SelectView(p, true);
            FocusView(p, true);
            Current = p;
            Unlock();
        }
    }

    /// <summary>Distributes the subviews' data, from the back, from consecutive slots of <paramref name="rec"/>.</summary>
    public override void SetData(ReadOnlySpan<object?> rec)
    {
        var i = 0;
        if (Last is not null)
        {
            var v = Last;
            do
            {
                v.SetData(rec[i..]);
                i += v.DataSize();
                v = v.Prev();
            } while (v != Last);
        }
    }

    /// <summary>
    /// Sets or clears <paramref name="aState"/>, propagating <c>sfActive</c> and
    /// <c>sfDragging</c> to all subviews, <c>sfFocused</c> to the current one, and
    /// <c>sfExposed</c> to the visible ones.
    /// </summary>
    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);

        if ((aState & (sfActive | sfDragging)) != 0)
        {
            Lock();
            ForEach(p => p.SetState(aState, enable));
            Unlock();
        }

        if ((aState & sfFocused) != 0)
            Current?.SetState(sfFocused, enable);

        if ((aState & sfExposed) != 0)
        {
            ForEach(p =>
            {
                if ((p.State & sfVisible) != 0)
                    p.SetState(sfExposed, enable);
            });
            if (!enable)
                FreeBuffer();
        }
    }

    /// <summary>Ends a <see cref="Lock"/>, drawing the group when the last one ends.</summary>
    public void Unlock()
    {
        if (LockFlag != 0 && --LockFlag == 0)
            DrawView();
    }

    /// <summary>
    /// Whether every subview is valid for <paramref name="command"/>. For
    /// <c>cmReleasedFocus</c>, only the current subview is asked, if it has <c>ofValidate</c>.
    /// </summary>
    public override bool Valid(ushort command)
    {
        if (command == cmReleasedFocus)
        {
            if (Current is not null && (Current.Options & ofValidate) != 0)
                return Current.Valid(command);
            return true;
        }

        return FirstThat(p => !p.Valid(command)) is null;
    }

    /// <summary>The current subview's help context, or the group's own if that is <c>hcNoContext</c>.</summary>
    public override ushort GetHelpCtx()
    {
        var h = hcNoContext;
        if (Current is not null)
            h = Current.GetHelpCtx();
        if (h == hcNoContext)
            h = base.GetHelpCtx();
        return h;
    }

    /// <summary>The subview <paramref name="index"/> steps after <see cref="Last"/> (1 is the frontmost).</summary>
    public TView? At(int index)
    {
        var temp = Last;
        index = (short)index;
        while (index-- > 0)
            temp = temp!.Next;
        return temp;
    }

    /// <summary>The first subview, from the front, for which <paramref name="func"/> returns true, or null.</summary>
    public TView? FirstThat(Func<TView, bool> func)
    {
        var temp = Last;
        if (temp is null)
            return null;
        do
        {
            temp = temp.Next!;
            if (func(temp))
                return temp;
        } while (temp != Last);
        return null;
    }

    /// <summary>
    /// Calls <paramref name="func"/> on every subview, from the front. The next view is read
    /// before each call, so <paramref name="func"/> may remove the view it is given.
    /// </summary>
    public void ForEach(Action<TView> func)
    {
        var term = Last;
        var temp = Last;
        if (temp is null)
            return;
        var next = temp.Next!;
        do
        {
            temp = next;
            next = temp.Next!;
            func(temp);
        } while (temp != term);
    }

    /// <summary>The position of <paramref name="p"/> from the front (1-based), or 0 if it is not a subview.</summary>
    public short IndexOf(TView? p)
    {
        if (Last is null)
            return 0;
        short index = 0;
        var temp = Last;
        do
        {
            index++;
            temp = temp.Next!;
        } while (temp != p && temp != Last);
        return temp != p ? (short)0 : index;
    }

    /// <summary>Unlinks <paramref name="p"/> from the list, without drawing.</summary>
    public void RemoveView(TView p)
    {
        if (Last is not null)
        {
            var s = Last;
            while (s.Next != p)
            {
                if (s.Next == Last)
                    return;
                s = s.Next!;
            }
            s.Next = p.Next;
            if (p == Last)
                Last = p == p.Next ? null : s;
        }
    }
}
