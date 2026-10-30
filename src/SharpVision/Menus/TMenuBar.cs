using SharpVision.Drawing;
using SharpVision.Objects;

using static SharpVision.Objects.Util;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Menus;

/// <summary>The horizontal menu bar at the top of the application.</summary>
/// Ported from include/tvision/menus.h (class TMenuBar) and source/tvision/tmenubar.cpp.
public class TMenuBar : TMenuView
{
    /// <summary>Creates a menu bar for <paramref name="menu"/>.</summary>
    public TMenuBar(TRect bounds, TMenu? menu) : base(bounds)
    {
        Menu = menu;
        GrowMode = (byte)gfGrowHiX;
        Options |= ofPreProcess;
    }

    /// <summary>Creates a menu bar whose entries are the submenus chained to <paramref name="menu"/>.</summary>
    public TMenuBar(TRect bounds, TSubMenu menu) : this(bounds, new TMenu(menu))
    { }

    /// <summary>Draws the entries, highlighting the current one.</summary>
    public override void Draw()
    {
        var b = new TDrawBuffer();
        TAttrPair cNormal = GetColor(0x0301);
        TAttrPair cSelect = GetColor(0x0604);
        TAttrPair cNormDisabled = GetColor(0x0202);
        TAttrPair cSelDisabled = GetColor(0x0505);
        b.MoveChar(0, ' ', cNormal, Size.X);
        if (Menu is not null)
        {
            var x = 1;
            for (var p = Menu.Items; p is not null; p = p.Next)
            {
                if (p.Name is null)
                    continue;
                var l = Cstrlen(p.Name);
                if (x + l < Size.X)
                {
                    TAttrPair color;
                    if (p.Disabled)
                        color = p == Current ? cSelDisabled : cNormDisabled;
                    else
                        color = p == Current ? cSelect : cNormal;
                    b.MoveChar(x, ' ', color, 1);
                    b.MoveCStr(x + 1, p.Name, color);
                    b.MoveChar(x + l + 1, ' ', color, 1);
                }
                x += l + 2;
            }
        }
        WriteBuf(0, 0, Size.X, 1, b);
    }

    /// <summary>The rectangle of the entry for <paramref name="item"/>.</summary>
    public override TRect GetItemRect(TMenuItem? item)
    {
        var r = new TRect(1, 0, 1, 1);
        var p = Menu!.Items;
        while (true)
        {
            r.A.X = r.B.X;
            if (p!.Name is not null)
                r.B.X += Cstrlen(p.Name) + 2;
            if (p == item)
                return r;
            p = p.Next;
        }
    }
}
