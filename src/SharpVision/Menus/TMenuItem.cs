using SharpVision.Events;
using SharpVision.Views;

using static SharpVision.Views.ViewFlags;

namespace SharpVision.Menus;

/// <summary>
/// An item in a menu: a command (with an optional hotkey and parameter text such as
/// "Alt-X"), a submenu (<see cref="Command"/> 0), or a separator line (<see cref="Name"/> null).
/// </summary>
/// <remarks>Items are chained with <c>+</c>: <c>new TMenuItem(...) + new TMenuItem(...)</c>.</remarks>
/// Ported from include/tvision/menus.h (TMenuItem, TSubMenu, TMenu), source/tvision/menu.cpp
/// and source/tvision/tmnuview.cpp (constructors).
public class TMenuItem
{
    /// <summary>The next item in the menu.</summary>
    public TMenuItem? Next;

    /// <summary>The item text, with <c>~</c> around the hotkey letter; null for a separator.</summary>
    public string? Name;

    /// <summary>The command, or 0 for a submenu.</summary>
    public ushort Command;

    /// <summary>Whether the item is disabled (its command is disabled).</summary>
    public bool Disabled;

    /// <summary>The shortcut key, or <c>kbNoKey</c>.</summary>
    public TKey KeyCode;

    /// <summary>The help context.</summary>
    public ushort HelpCtx;

    /// <summary>The text shown on the right, such as "F3" (command items only).</summary>
    public string? Param;

    /// <summary>The submenu (submenu items only).</summary>
    public TMenu? SubMenu;

    /// <summary>Creates a command item.</summary>
    public TMenuItem(string? name, ushort command, TKey key, ushort helpCtx = hcNoContext, string? param = null, TMenuItem? next = null)
    {
        Name = name;
        Command = command;
        Disabled = !TView.CommandEnabled(command);
        KeyCode = key;
        HelpCtx = helpCtx;
        Param = string.IsNullOrEmpty(param) ? null : param;
        Next = next;
    }

    /// <summary>Creates a submenu item.</summary>
    public TMenuItem(string? name, TKey key, TMenu? subMenu, ushort helpCtx = hcNoContext, TMenuItem? next = null)
    {
        Name = name;
        Command = 0;
        Disabled = !TView.CommandEnabled(Command);
        KeyCode = key;
        HelpCtx = helpCtx;
        SubMenu = subMenu;
        Next = next;
    }

    /// <summary>Sets the next item.</summary>
    public void Append(TMenuItem? next) => Next = next;

    /// <summary>Creates a separator line.</summary>
    public static TMenuItem NewLine() => new(null, 0, new TKey(0), hcNoContext, null, null);

    /// <summary>Appends <paramref name="i2"/> at the end of the list starting at <paramref name="i1"/>.</summary>
    public static TMenuItem operator +(TMenuItem i1, TMenuItem i2)
    {
        var cur = i1;
        while (cur.Next is not null)
            cur = cur.Next;
        cur.Next = i2;
        return i1;
    }
}

/// <summary>A submenu item whose items are added with <c>+</c>.</summary>
/// <remarks>Creates an empty submenu.</remarks>
public class TSubMenu(string name, TKey key, ushort helpCtx = hcNoContext) : TMenuItem(name, key, new TMenu(), helpCtx)
{

    /// <summary>Appends <paramref name="i"/> to the items of the last submenu in the list starting at <paramref name="s"/>.</summary>
    public static TSubMenu operator +(TSubMenu s, TMenuItem i)
    {
        var sub = s;
        while (sub.Next is not null)
            sub = (TSubMenu)sub.Next;
        var menu = sub.SubMenu!;
        if (menu.Items is null)
        {
            menu.Items = i;
            menu.Deflt = i;
        }
        else
        {
            var cur = menu.Items;
            while (cur.Next is not null)
                cur = cur.Next;
            cur.Next = i;
        }
        return s;
    }

    /// <summary>Appends the submenu <paramref name="s2"/> after the last submenu of <paramref name="s1"/>.</summary>
    public static TSubMenu operator +(TSubMenu s1, TSubMenu s2)
    {
        TMenuItem cur = s1;
        while (cur.Next is not null)
            cur = cur.Next;
        cur.Next = s2;
        return s1;
    }
}

/// <summary>A list of menu items and the item selected by default.</summary>
public class TMenu
{
    /// <summary>The first item.</summary>
    public TMenuItem? Items;

    /// <summary>The item selected when the menu opens.</summary>
    public TMenuItem? Deflt;

    /// <summary>Creates an empty menu.</summary>
    public TMenu()
    {
    }

    /// <summary>Creates a menu whose default item is the first one.</summary>
    public TMenu(TMenuItem itemList)
    {
        Items = itemList;
        Deflt = itemList;
    }

    /// <summary>Creates a menu with a given default item.</summary>
    public TMenu(TMenuItem itemList, TMenuItem theDefault)
    {
        Items = itemList;
        Deflt = theDefault;
    }
}
