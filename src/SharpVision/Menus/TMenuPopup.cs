using System;

using SharpVision.App;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Objects.Util;

namespace SharpVision.Menus;

/// <summary>
/// A menu box that pops up on its own (not from a menu bar), e.g. as a context menu. Shortcut letters
/// and hotkeys pick an item directly by putting its command.
/// </summary>
/// Ported from source/tvision/tmenupop.cpp, source/tvision/popupmnu.cpp (popupMenu),
/// include/tvision/menus.h (class TMenuPopup), include/tvision/util.h (popupMenu).
/// <remarks>Palette layout: as <see cref="TMenuBox"/>.</remarks>
public class TMenuPopup : TMenuBox
{
    /// <summary>Creates a popup for <paramref name="aMenu"/>, sized to fit within <paramref name="bounds"/>.</summary>
    public TMenuPopup(TRect bounds, TMenu? aMenu, TMenuView? aParent = null)
        : base(bounds, aMenu, aParent)
    {
        PutClickEventOnExit = false;
    }

    /// <summary>Runs the menu without highlighting a default item.</summary>
    public override ushort Execute()
    {
        // Do not highlight the default entry, because it would look ugly.
        Menu!.Deflt = null;
        return base.Execute();
    }

    /// <summary>Puts the command of the item whose Ctrl letter or hotkey was pressed; swallows other Alt keys.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        if (ev.What == evKeyDown)
        {
            var p = FindItem(GetCtrlChar(ev.KeyDown.KeyCode));
            p ??= HotKey(ev.KeyDown.KeyCode);
            if (p is not null && CommandEnabled(p.Command))
            {
                ev.What = evCommand;
                ev.Message.Command = p.Command;
                ev.Message.InfoPtr = null;
                PutEvent(ref ev);
                ClearEvent(ref ev);
            }
            else if (GetAltChar(ev.KeyDown.KeyCode) != '\0')
                ClearEvent(ref ev);
        }
        base.HandleEvent(ref ev);
    }

    /// <summary>
    /// Shows <paramref name="aMenu"/> as a popup below <paramref name="where"/> (absolute coordinates) on the
    /// application and returns the chosen command, or 0. If <paramref name="receiver"/> is given, the command
    /// is also put to it as an <c>evCommand</c> event.
    /// </summary>
    public static ushort PopupMenu(TPoint where, TMenuItem aMenu, TGroup? receiver = null)
    {
        ushort res = 0;
        var app = TProgram.Application;
        if (app is not null)
        {
            var p = app.MakeLocal(where);
            var menuPopup = new TMenuPopup(new TRect(p, p), new TMenu(aMenu));
            AutoPlacePopup(app, menuPopup, p);
            // Execute and dispose the menu.
            res = app.ExecView(menuPopup);
            TObject.Destroy(menuPopup);

            // Generate an event.
            if (res != 0 && receiver is not null)
            {
                var ev = new TEvent { What = evCommand };
                ev.Message.Command = res;
                receiver.PutEvent(ref ev);
            }
        }
        return res;
    }

    // Pre: the popup was constructed with bounds = TRect(p, p).
    private static void AutoPlacePopup(TGroup app, TMenuPopup m, TPoint p)
    {
        // Initially, the menu is placed above 'p'. So we need to move it.
        var r = m.GetBounds();
        // But we must ensure that the popup does not go beyond the desktop's
        // bottom right corner, for usability.
        var d = app.Size - p;
        r.Move(Math.Min(m.Size.X, d.X), Math.Min(m.Size.Y + 1, d.Y));
        // If the popup then contains 'p', try to move it to a better place.
        if (r.Contains(p) && r.B.Y - r.A.Y < p.Y)
            r.Move(0, -(r.B.Y - p.Y));
        m.SetBounds(r);
    }
}
