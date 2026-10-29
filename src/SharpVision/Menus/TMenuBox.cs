// Ported from include/tvision/menus.h (class TMenuBox), source/tvision/tmenubox.cpp and
// source/tvision/tvtext1.cpp (frameChars).

using System;

using SharpVision.Drawing;
using SharpVision.Objects;

using static SharpVision.Objects.Util;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Menus;

/// <summary>A framed, vertical menu (a submenu or a popup menu).</summary>
public class TMenuBox : TMenuView
{
    // Groups of 5 characters: top line (0), bottom line (5), item line (10), separator (15).
    private const string FrameChars = " ┌─┐  └─┘  │ │  ├─┤ ";

    private TAttrPair _cNormal;
    private TAttrPair _color;

    /// <summary>Creates a menu box sized to fit <paramref name="menu"/> within <paramref name="bounds"/>.</summary>
    public TMenuBox(TRect bounds, TMenu? menu, TMenuView? parentMenu)
        : base(GetRect(bounds, menu), menu, parentMenu)
    {
        State |= sfShadow;
        Options |= ofPreProcess;
    }

    private static TRect GetRect(TRect bounds, TMenu? menu)
    {
        var w = 10;
        var h = 2;
        if (menu is not null)
        {
            for (var p = menu.Items; p is not null; p = p.Next)
            {
                if (p.Name is not null)
                {
                    var l = Cstrlen(p.Name) + 6;
                    if (p.Command == 0)
                        l += 3;
                    else if (p.Param is not null)
                        l += Cstrlen(p.Param) + 2;
                    w = Math.Max(l, w);
                }
                h++;
            }
        }
        var r = bounds;
        if (r.A.X + w < r.B.X)
            r.B.X = r.A.X + w;
        else
            r.A.X = r.B.X - w;
        if (r.A.Y + h < r.B.Y)
            r.B.Y = r.A.Y + h;
        else
            r.A.Y = r.B.Y - h;
        return r;
    }

    private void FrameLine(TDrawBuffer b, int n)
    {
        b.MoveBuf(0, FrameChars.AsSpan(n), _cNormal, 2);
        b.MoveChar(2, FrameChars[n + 2], _color, Size.X - 4);
        b.MoveBuf(Size.X - 2, FrameChars.AsSpan(n + 3), _cNormal, 2);
    }

    /// <summary>Draws the frame and the items, highlighting the current one.</summary>
    public override void Draw()
    {
        var b = new TDrawBuffer();
        _cNormal = GetColor(0x0301);
        TAttrPair cSelect = GetColor(0x0604);
        TAttrPair cNormDisabled = GetColor(0x0202);
        TAttrPair cSelDisabled = GetColor(0x0505);
        var y = 0;
        _color = _cNormal;
        FrameLine(b, 0);
        WriteBuf(0, y++, Size.X, 1, b);
        if (Menu is not null)
        {
            for (var p = Menu.Items; p is not null; p = p.Next)
            {
                _color = _cNormal;
                if (p.Name is null)
                    FrameLine(b, 15);
                else
                {
                    if (p.Disabled)
                        _color = p == Current ? cSelDisabled : cNormDisabled;
                    else if (p == Current)
                        _color = cSelect;
                    FrameLine(b, 10);
                    b.MoveCStr(3, p.Name, _color);
                    if (p.Command == 0)
                        b.PutChar(Size.X - 4, '►');
                    else if (p.Param is not null)
                        b.MoveCStr(Size.X - 3 - Cstrlen(p.Param), p.Param, _color);
                }
                WriteBuf(0, y++, Size.X, 1, b);
            }
        }
        _color = _cNormal;
        FrameLine(b, 5);
        WriteBuf(0, y, Size.X, 1, b);
    }

    /// <summary>The rectangle of the line for <paramref name="item"/>.</summary>
    public override TRect GetItemRect(TMenuItem? item)
    {
        var y = 1;
        var p = Menu!.Items;
        while (p != item)
        {
            y++;
            p = p!.Next;
        }
        return new TRect(2, y, Size.X - 2, y + 1);
    }
}
