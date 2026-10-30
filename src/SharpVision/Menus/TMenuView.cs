using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Objects.Util;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Menus;

/// <summary>The base of menu bars and menu boxes: runs a menu and returns the chosen command.</summary>
/// <remarks>
/// Palette layout: 1 = normal text, 2 = disabled text, 3 = shortcut text, 4 = normal selection,
/// 5 = disabled selection, 6 = shortcut selection.
/// </remarks>
/// Ported from include/tvision/menus.h (class TMenuView) and source/tvision/tmnuview.cpp.
public class TMenuView : TView
{
    private static readonly TPalette _palette = new([0x02, 0x03, 0x04, 0x05, 0x06, 0x07]);

    private enum MenuAction
    {
        DoNothing,
        DoSelect,
        DoReturn,
    }

    /// <summary>The menu that opened this one, or null.</summary>
    protected TMenuView? ParentMenu;

    /// <summary>The menu shown.</summary>
    protected TMenu? Menu;

    /// <summary>The highlighted item, or null.</summary>
    protected TMenuItem? Current;

    /// <summary>Whether a click outside the menu that closes it is passed on to the view under the mouse.</summary>
    protected bool PutClickEventOnExit = true;

    /// <summary>Creates a menu view for <paramref name="menu"/>.</summary>
    public TMenuView(TRect bounds, TMenu? menu, TMenuView? parent = null)
        : base(bounds)
    {
        ParentMenu = parent;
        Menu = menu;
        EventMask |= evBroadcast;
    }

    /// <summary>Creates a menu view without a menu.</summary>
    public TMenuView(TRect bounds)
        : this(bounds, null)
    {
    }

    private void TrackMouse(in TEvent e, ref bool mouseActive)
    {
        var mouse = MakeLocal(e.Mouse.Where);
        for (Current = Menu!.Items; Current is not null; Current = Current.Next)
        {
            if (GetItemRect(Current).Contains(mouse))
            {
                mouseActive = true;
                return;
            }
        }
    }

    private void NextItem()
    {
        if ((Current = Current!.Next) is null)
            Current = Menu!.Items;
    }

    private void PrevItem()
    {
        var p = Current;
        if (p == Menu!.Items)
            p = null;
        do
        {
            NextItem();
        } while (Current!.Next != p);
    }

    private void TrackKey(bool findNext)
    {
        if (Current is null)
        {
            Current = Menu!.Items;
            if (!findNext)
                PrevItem();
            if (Current!.Name is not null)
                return;
        }

        do
        {
            if (findNext)
                NextItem();
            else
                PrevItem();
        } while (Current!.Name is null);
    }

    private bool MouseInOwner(in TEvent e)
    {
        if (ParentMenu is null)
            return false;
        var mouse = ParentMenu.MakeLocal(e.Mouse.Where);
        return ParentMenu.GetItemRect(ParentMenu.Current).Contains(mouse);
    }

    private bool MouseInMenus(in TEvent e)
    {
        var p = ParentMenu;
        while (p is not null && !p.MouseInView(e.Mouse.Where))
            p = p.ParentMenu;
        return p is not null;
    }

    private TMenuView TopMenu()
    {
        var p = this;
        while (p.ParentMenu is not null)
            p = p.ParentMenu;
        return p;
    }

    /// <summary>Runs the menu modally, tracking the mouse and keyboard, and returns the chosen command or 0.</summary>
    public override ushort Execute()
    {
        var autoSelect = false;
        var firstEvent = true;
        MenuAction action;
        ushort result = 0;
        TMenuItem? itemShown = null;
        TMenuItem? lastTargetItem = null;
        var e = new TEvent();
        var mouseActive = false;

        Current = Menu!.Deflt;

        do
        {
            action = MenuAction.DoNothing;
            GetEvent(ref e);
            switch (e.What)
            {
                case evMouseDown:
                    if (MouseInView(e.Mouse.Where) || MouseInOwner(e))
                    {
                        TrackMouse(e, ref mouseActive);
                        // autoSelect opens the selected submenu directly on mouse down, except
                        // when the submenu was just closed by clicking on its name, or when
                        // this is not a menu bar.
                        if (Size.Y == 1)
                            autoSelect = Current is null || lastTargetItem != Current;
                        // A submenu closes on a click on the parent menu, unless it was just opened.
                        else if (!firstEvent && MouseInOwner(e))
                            action = MenuAction.DoReturn;
                    }
                    else
                    {
                        // A click outside the menu closes it; let the event reach the view recovering focus.
                        if (PutClickEventOnExit)
                            PutEvent(ref e);
                        action = MenuAction.DoReturn;
                    }
                    break;
                case evMouseUp:
                    TrackMouse(e, ref mouseActive);
                    if (MouseInOwner(e))
                        Current = Menu.Deflt;
                    else if (Current is not null)
                    {
                        if (Current.Name is not null)
                        {
                            if (Current != lastTargetItem)
                                action = MenuAction.DoSelect;
                            else if (Size.Y == 1)
                                action = MenuAction.DoReturn; // A menu bar entry was closed: stop listening.
                            else
                            {
                                // Mouse up won't reopen a submenu just closed by clicking on its name,
                                // but the next one will.
                                action = MenuAction.DoNothing;
                                lastTargetItem = null;
                            }
                        }
                    }
                    else if (mouseActive && !MouseInView(e.Mouse.Where))
                        action = MenuAction.DoReturn;
                    else if (Size.Y != 1)
                    {
                        // Mouse up inside the box but not on an item (a margin or a separator)
                        // highlights the default or first item (Turbo Vision 2.0).
                        Current = Menu.Deflt ?? Menu.Items;
                        action = MenuAction.DoNothing;
                    }
                    break;
                case evMouseMove:
                    if (e.Mouse.Buttons != 0)
                    {
                        TrackMouse(e, ref mouseActive);
                        if (!(MouseInView(e.Mouse.Where) || MouseInOwner(e)) && MouseInMenus(e))
                            action = MenuAction.DoReturn;
                        // A menu bar entry closed by clicking on its name stays highlighted until mouse
                        // up; dragging to another entry then opens it.
                        else if (Size.Y == 1 && mouseActive && Current != lastTargetItem)
                            autoSelect = true;
                    }
                    break;
                case evKeyDown:
                    switch (e.KeyDown.KeyCode)
                    {
                        case kbUp:
                        case kbDown:
                            if (Size.Y != 1)
                                TrackKey(e.KeyDown.KeyCode == kbDown);
                            else if (e.KeyDown.KeyCode == kbDown)
                                autoSelect = true;
                            break;
                        case kbLeft:
                        case kbRight:
                            if (Size.Y == 1)
                                TrackKey(e.KeyDown.KeyCode == kbRight);
                            else if (ParentMenu is not null)
                                action = MenuAction.DoReturn;
                            break;
                        case kbHome:
                        case kbEnd:
                            if (Size.Y != 1)
                            {
                                Current = Menu.Items;
                                if (e.KeyDown.KeyCode == kbEnd)
                                    TrackKey(false);
                            }
                            break;
                        case kbEnter:
                            if (Size.Y == 1)
                                autoSelect = true;
                            action = MenuAction.DoSelect;
                            break;
                        case kbEsc:
                            action = MenuAction.DoReturn;
                            if (ParentMenu is null || ParentMenu.Size.Y != 1)
                                ClearEvent(ref e);
                            break;
                        default:
                            var target = this;
                            TMenuItem? p;
                            if (GetAltCharStr(e.KeyDown).Length != 0)
                            {
                                target = TopMenu();
                                p = target.FindAltShortcut(e.KeyDown);
                            }
                            else
                                p = FindItem(e.KeyDown.GetText());
                            if (p is null)
                            {
                                p = TopMenu().HotKey(e.KeyDown);
                                if (p is not null && CommandEnabled(p.Command))
                                {
                                    result = p.Command;
                                    action = MenuAction.DoReturn;
                                }
                            }
                            else if (target == this)
                            {
                                if (Size.Y == 1)
                                    autoSelect = true;
                                action = MenuAction.DoSelect;
                                Current = p;
                            }
                            else if (ParentMenu != target || ParentMenu.Current != p)
                                action = MenuAction.DoReturn;
                            break;
                    }
                    break;
                case evCommand:
                    if (e.Message.Command == cmMenu)
                    {
                        autoSelect = false;
                        lastTargetItem = null;
                        if (ParentMenu is not null)
                            action = MenuAction.DoReturn;
                    }
                    else
                        action = MenuAction.DoReturn;
                    break;
            }

            // A submenu closed by clicking on its name opens again the next time it is hovered over.
            if (lastTargetItem != Current)
                lastTargetItem = null;

            if (itemShown != Current)
            {
                itemShown = Current;
                DrawView();
            }

            if ((action == MenuAction.DoSelect || (action == MenuAction.DoNothing && autoSelect)) &&
                Current is not null && Current.Name is not null)
            {
                if (Current.Command == 0 && !Current.Disabled)
                {
                    if ((e.What & (evMouseDown | evMouseMove)) != 0)
                        PutEvent(ref e);
                    var r = GetItemRect(Current);
                    r.A.X += Origin.X;
                    r.A.Y = r.B.Y + Origin.Y;
                    r.B = Owner!.Size;
                    if (Size.Y == 1)
                        r.A.X--;
                    var subView = TopMenu().NewSubView(r, Current.SubMenu, this);
                    result = Owner.ExecView(subView);
                    Destroy(subView);
                    lastTargetItem = Current;
                    Menu.Deflt = Current;
                }
                else if (action == MenuAction.DoSelect)
                    result = Current.Command;
            }

            if (result != 0 && CommandEnabled(result))
            {
                action = MenuAction.DoReturn;
                ClearEvent(ref e);
            }
            else
                result = 0;

            firstEvent = false;
        } while (action != MenuAction.DoReturn);

        if (e.What != evNothing && (ParentMenu is not null || e.What == evCommand))
            PutEvent(ref e);
        if (Current is not null)
        {
            Menu.Deflt = Current;
            Current = null;
            DrawView();
        }
        return result;
    }

    /// <summary>The enabled item whose hotkey letter is <paramref name="shortcut"/>, or null.</summary>
    public TMenuItem? FindItem(char shortcut) => FindItem([shortcut]);

    /// <summary>The enabled item whose hotkey text is <paramref name="shortcut"/> (ignoring case), or null.</summary>
    public TMenuItem? FindItem(System.ReadOnlySpan<char> shortcut)
    {
        for (var p = Menu!.Items; p is not null; p = p.Next)
        {
            var hotKey = HotKeyStr(p.Name);
            if (!p.Disabled && !hotKey.IsEmpty && TText.EqualsIgnoreCase(shortcut, hotKey))
                return p;
        }
        return null;
    }

    private TMenuItem? FindAltShortcut(in KeyDownEvent keyDown)
    {
        TMenuItem? p = null;
        // First, try to find an item that matches the event's text, then its key code.
        if (!keyDown.GetText().IsEmpty)
            p = FindItem(GetAltCharStr(keyDown));
        if (p is null)
        {
            var c = GetAltChar(keyDown.KeyCode);
            if (c != '\0')
                p = FindItem(c);
        }
        return p;
    }

    /// <summary>The rectangle of <paramref name="item"/> in view coordinates. Empty by default.</summary>
    public virtual TRect GetItemRect(TMenuItem? item) => new(0, 0, 0, 0);

    /// <summary>The help context of the highlighted item in this menu or the closest parent with one.</summary>
    public override ushort GetHelpCtx()
    {
        TMenuView? c = this;
        while (c is not null &&
               (c.Current is null || c.Current.HelpCtx == hcNoContext || c.Current.Name is null))
            c = c.ParentMenu;
        return c is not null ? c.Current!.HelpCtx : HelpCtx;
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    private bool UpdateMenu(TMenu? menu)
    {
        var res = false;
        if (menu is not null)
        {
            for (var p = menu.Items; p is not null; p = p.Next)
            {
                if (p.Name is null)
                    continue;
                if (p.Command == 0)
                {
                    if (UpdateMenu(p.SubMenu))
                        res = true;
                }
                else
                {
                    var commandState = CommandEnabled(p.Command);
                    if (p.Disabled == commandState)
                    {
                        p.Disabled = !commandState;
                        res = true;
                    }
                }
            }
        }
        return res;
    }

    private void DoASelect(ref TEvent ev)
    {
        PutEvent(ref ev);
        ev.Message.Command = Owner!.ExecView(this);
        if (ev.Message.Command != 0 && CommandEnabled(ev.Message.Command))
        {
            ev.What = evCommand;
            ev.Message.InfoPtr = null;
            PutEvent(ref ev);
        }
        ClearEvent(ref ev);
    }

    /// <summary>Opens the menu on a click, an Alt+hotkey or <c>cmMenu</c>, runs item shortcuts, and tracks enabled commands.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        if (Menu is null)
            return;
        switch (ev.What)
        {
            case evMouseDown:
                DoASelect(ref ev);
                break;
            case evKeyDown:
                if (FindAltShortcut(ev.KeyDown) is not null)
                    DoASelect(ref ev);
                else
                {
                    var p = HotKey(ev.KeyDown);
                    if (p is not null && CommandEnabled(p.Command))
                    {
                        ev.What = evCommand;
                        ev.Message.Command = p.Command;
                        ev.Message.InfoPtr = null;
                        PutEvent(ref ev);
                        ClearEvent(ref ev);
                    }
                }
                break;
            case evCommand:
                if (ev.Message.Command == cmMenu)
                    DoASelect(ref ev);
                break;
            case evBroadcast:
                if (ev.Message.Command == cmCommandSetChanged && UpdateMenu(Menu))
                    DrawView();
                break;
        }
    }

    private static TMenuItem? FindHotKey(TMenuItem? p, TKey key)
    {
        for (; p is not null; p = p.Next)
        {
            if (p.Name is null)
                continue;
            if (p.Command == 0)
            {
                var t = FindHotKey(p.SubMenu!.Items, key);
                if (t is not null)
                    return t;
            }
            else if (!p.Disabled && p.KeyCode != kbNoKey && p.KeyCode == key)
                return p;
        }
        return null;
    }

    /// <summary>The enabled item, in this menu or its submenus, whose shortcut is <paramref name="key"/>, or null.</summary>
    public TMenuItem? HotKey(TKey key) => FindHotKey(Menu!.Items, key);

    /// <summary>Creates the view for a submenu. A <see cref="TMenuBox"/> by default.</summary>
    public virtual TMenuView NewSubView(TRect bounds, TMenu? menu, TMenuView? parentMenu) =>
        new TMenuBox(bounds, menu, parentMenu);
}
