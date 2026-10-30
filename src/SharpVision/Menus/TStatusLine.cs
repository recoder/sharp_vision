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

// Ported from include/tvision/menus.h (TStatusItem, TStatusDef, TStatusLine),
// source/tvision/menu.cpp (operators) and source/tvision/tstatusl.cpp.

/// <summary>
/// An entry in the status line: text (null for a key binding that is not shown), a
/// shortcut key and a command.
/// </summary>
/// <remarks>Creates a status item.</remarks>
public class TStatusItem(string? text, TKey key, ushort command, TStatusItem? next = null)
{
    /// <summary>The next item.</summary>
    public TStatusItem? Next = next;

    /// <summary>The text, with <c>~</c> around the highlighted part; null if not shown.</summary>
    public string? Text = text;

    /// <summary>The shortcut key.</summary>
    public TKey KeyCode = key;

    /// <summary>The command.</summary>
    public ushort Command = command;
}

/// <summary>The status items shown while the help context is in [<see cref="Min"/>, <see cref="Max"/>].</summary>
/// <remarks>Built with <c>+</c>: <c>new TStatusDef(0, 0xFFFF) + new TStatusItem(...) + ...</c>.</remarks>
/// <remarks>Creates a status definition.</remarks>
public class TStatusDef(ushort min, ushort max, TStatusItem? items = null, TStatusDef? next = null)
{
    /// <summary>The next definition.</summary>
    public TStatusDef? Next = next;

    /// <summary>The lowest help context this definition applies to.</summary>
    public ushort Min = min;

    /// <summary>The highest help context this definition applies to.</summary>
    public ushort Max = max;

    /// <summary>The items.</summary>
    public TStatusItem? Items = items;

    /// <summary>Appends <paramref name="s2"/> to the items of the last definition in the list starting at <paramref name="s1"/>.</summary>
    public static TStatusDef operator +(TStatusDef s1, TStatusItem s2)
    {
        var def = s1;
        while (def.Next is not null)
            def = def.Next;
        if (def.Items is null)
            def.Items = s2;
        else
        {
            var cur = def.Items;
            while (cur.Next is not null)
                cur = cur.Next;
            cur.Next = s2;
        }
        return s1;
    }

    /// <summary>Appends the definition <paramref name="s2"/> to the list starting at <paramref name="s1"/>.</summary>
    public static TStatusDef operator +(TStatusDef s1, TStatusDef s2)
    {
        var cur = s1;
        while (cur.Next is not null)
            cur = cur.Next;
        cur.Next = s2;
        return s1;
    }
}

/// <summary>
/// The status line at the bottom of the application: shows the items for the current help
/// context, turns their keys and clicks into commands, and shows a hint.
/// </summary>
/// <remarks>
/// Palette layout: 1 = normal text, 2 = disabled text, 3 = shortcut text, 4 = normal selection,
/// 5 = disabled selection, 6 = shortcut selection.
/// </remarks>
public class TStatusLine : TView
{
    private const string HintSeparator = "│ ";
    private static readonly TPalette _palette = new([0x02, 0x03, 0x04, 0x05, 0x06, 0x07]);

    /// <summary>The items shown.</summary>
    protected TStatusItem? Items;

    /// <summary>The definitions.</summary>
    protected TStatusDef? Defs;

    /// <summary>Creates a status line from a list of definitions.</summary>
    public TStatusLine(TRect bounds, TStatusDef defs) : base(bounds)
    {
        Defs = defs;
        Options |= ofPreProcess;
        EventMask |= evBroadcast;
        GrowMode = (byte)(gfGrowLoY | gfGrowHiX | gfGrowHiY);
        FindItems();
    }

    /// <summary>Draws the items and the hint.</summary>
    public override void Draw() => DrawSelect(null);

    private void DrawSelect(TStatusItem? selected)
    {
        var b = new TDrawBuffer();
        TAttrPair cNormal = GetColor(0x0301);
        TAttrPair cSelect = GetColor(0x0604);
        TAttrPair cNormDisabled = GetColor(0x0202);
        TAttrPair cSelDisabled = GetColor(0x0505);
        b.MoveChar(0, ' ', cNormal, Size.X);
        var i = 0;
        for (var t = Items; t is not null; t = t.Next)
        {
            if (t.Text is null)
                continue;
            var l = Cstrlen(t.Text);
            if (i + l < Size.X)
            {
                TAttrPair color;
                if (CommandEnabled(t.Command))
                    color = t == selected ? cSelect : cNormal;
                else
                    color = t == selected ? cSelDisabled : cNormDisabled;
                b.MoveChar(i, ' ', color, 1);
                b.MoveCStr(i + 1, t.Text, color);
                b.MoveChar(i + l + 1, ' ', color, 1);
            }
            i += l + 2;
        }
        if (i < Size.X - 2)
        {
            var hintText = Hint(HelpCtx);
            if (!string.IsNullOrEmpty(hintText))
            {
                b.MoveStr(i, HintSeparator, cNormal);
                i += 2;
                b.MoveStr(i, hintText, cNormal, Size.X - i);
            }
        }
        WriteLine(0, 0, Size.X, 1, b);
    }

    private void FindItems()
    {
        var p = Defs;
        while (p is not null && (HelpCtx < p.Min || HelpCtx > p.Max))
            p = p.Next;
        Items = p?.Items;
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    private TStatusItem? ItemMouseIsIn(TPoint mouse)
    {
        if (mouse.Y != 0)
            return null;
        var i = 0;
        for (var t = Items; t is not null; t = t.Next)
        {
            if (t.Text is not null)
            {
                var k = i + Cstrlen(t.Text) + 2;
                if (mouse.X >= i && mouse.X < k)
                    return t;
                i = k;
            }
        }
        return null;
    }

    /// <summary>Turns clicks on items and their shortcut keys into commands.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        switch (ev.What)
        {
            case evMouseDown:
                {
                    TStatusItem? t = null;
                    do
                    {
                        var mouse = MakeLocal(ev.Mouse.Where);
                        if (t != ItemMouseIsIn(mouse))
                            DrawSelect(t = ItemMouseIsIn(mouse));
                    } while (MouseEvent(ref ev, evMouseMove));
                    if (t is not null && CommandEnabled(t.Command))
                    {
                        ev.What = evCommand;
                        ev.Message.Command = t.Command;
                        ev.Message.InfoPtr = null;
                        PutEvent(ref ev);
                    }
                    ClearEvent(ref ev);
                    DrawView();
                    break;
                }
            case evKeyDown:
                if (ev.KeyDown.KeyCode != kbNoKey)
                {
                    for (var t = Items; t is not null; t = t.Next)
                    {
                        if ((TKey)ev.KeyDown == t.KeyCode && CommandEnabled(t.Command))
                        {
                            ev.What = evCommand;
                            ev.Message.Command = t.Command;
                            ev.Message.InfoPtr = null;
                            return;
                        }
                    }
                }
                break;
            case evBroadcast:
                if (ev.Message.Command == cmCommandSetChanged)
                    DrawView();
                break;
        }
    }

    /// <summary>The hint for help context <paramref name="helpCtx"/>. Empty by default.</summary>
    public virtual string Hint(ushort helpCtx) => "";

    /// <summary>Switches to the items for the top view's help context, redrawing if it changed.</summary>
    public void Update()
    {
        var p = TopView();
        var h = p?.GetHelpCtx() ?? hcNoContext;
        if (HelpCtx != h)
        {
            HelpCtx = h;
            FindItems();
            DrawView();
        }
    }
}
