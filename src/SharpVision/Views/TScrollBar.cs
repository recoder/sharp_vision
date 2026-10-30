// Ported from source/tvision/tscrlbar.cpp, source/tvision/tvtext1.cpp (vChars, hChars),
// include/tvision/views.h (class TScrollBar).

using System;

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
/// A vertical (width 1) or horizontal scroll bar with arrows, page areas and a thumb. Owners
/// learn about changes through <c>cmScrollBarChanged</c> and <c>cmScrollBarClicked</c> broadcasts.
/// </summary>
/// <remarks>Palette layout: 1 = page areas, 2 = arrows, 3 = indicator.</remarks>
public class TScrollBar : TView
{
    private static readonly TPalette _palette = new([0x04, 0x05, 0x05]);
    private static readonly char[] _vChars = ['▲', '▼', '▒', '■', '▓'];
    private static readonly char[] _hChars = ['◄', '►', '▒', '■', '▓'];

    // Shared by HandleEvent and GetPartCode, as upstream's file-level statics.
    private static TPoint _mouse;
    private static int _p, _s;
    private static TRect _extent;

    /// <summary>The current value.</summary>
    public int Value;

    /// <summary>The arrows, page area, thumb and the page area shown when the range is empty.</summary>
    public char[] Chars;

    /// <summary>The minimum value.</summary>
    public int MinVal;

    /// <summary>The maximum value.</summary>
    public int MaxVal;

    /// <summary>The step for page areas and PgUp/PgDn.</summary>
    public int PgStep;

    /// <summary>The step for arrows.</summary>
    public int ArStep;

    /// <summary>Creates a scroll bar; vertical when <paramref name="bounds"/> is one column wide.</summary>
    public TScrollBar(TRect bounds)
        : base(bounds)
    {
        PgStep = 1;
        ArStep = 1;
        if (Size.X == 1)
        {
            GrowMode = gfGrowLoX | gfGrowHiX | gfGrowHiY;
            Chars = (char[])_vChars.Clone();
        }
        else
        {
            GrowMode = gfGrowLoY | gfGrowHiX | gfGrowHiY;
            Chars = (char[])_hChars.Clone();
        }
        EventMask |= evMouseWheel;
    }

    /// <inheritdoc />
    public override void Draw() => DrawPos(GetPos());

    /// <summary>Draws the bar with the thumb at <paramref name="pos"/>.</summary>
    public void DrawPos(int pos)
    {
        var b = new TDrawBuffer();

        var s = GetSize() - 1;
        b.MoveChar(0, Chars[0], GetColor(2), 1);
        if (MaxVal == MinVal)
            b.MoveChar(1, Chars[4], GetColor(1), s - 1);
        else
        {
            b.MoveChar(1, Chars[2], GetColor(1), s - 1);
            b.MoveChar(pos, Chars[3], GetColor(3), 1);
        }

        b.MoveChar(s, Chars[1], GetColor(2), 1);
        WriteBuf(0, 0, Size.X, Size.Y, b);
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    /// <summary>The thumb position for the current value, between the arrows.</summary>
    public int GetPos()
    {
        var r = MaxVal - MinVal;
        if (r == 0)
            return 1;
        return (int)(((long)(Value - MinVal) * (GetSize() - 3) + (r >> 1)) / r) + 1;
    }

    /// <summary>The bar's length, at least 3.</summary>
    public int GetSize() => Math.Max(3, Size.X == 1 ? Size.Y : Size.X);

    private int GetPartCode()
    {
        var part = -1;
        if (_extent.Contains(_mouse))
        {
            var mark = Size.X == 1 ? _mouse.Y : _mouse.X;

            if (mark == _p)
                part = sbIndicator;
            else
            {
                if (mark < 1)
                    part = sbLeftArrow;
                else if (mark < _p)
                    part = sbPageLeft;
                else if (mark < _s)
                    part = sbPageRight;
                else
                    part = sbRightArrow;

                if (Size.X == 1)
                    part += 4;
            }
        }
        return part;
    }

    /// <summary>Scrolls on clicks, drags, the mouse wheel and (when visible) the arrow and page keys.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        int i = 0, clickPart, step = 0;

        base.HandleEvent(ref ev);
        switch (ev.What)
        {
            case evMouseWheel:
                if ((State & sfVisible) != 0)
                {
                    if (Size.X == 1)
                    {
                        switch (ev.Mouse.Wheel)
                        {
                            case mwUp: step = -ArStep; break;
                            case mwDown: step = ArStep; break;
                        }
                    }
                    else
                    {
                        switch (ev.Mouse.Wheel)
                        {
                            case mwLeft: step = -ArStep; break;
                            case mwRight: step = ArStep; break;
                        }
                    }
                }
                if (step != 0)
                {
                    // E.g. when the bar is associated to a TListViewer, this message
                    // causes it to become selected.
                    Message(Owner, evBroadcast, cmScrollBarClicked, this);
                    SetValue(Value + 3 * step);
                    ClearEvent(ref ev);
                }
                break;
            case evMouseDown:
                Message(Owner, evBroadcast, cmScrollBarClicked, this); // Clicked()
                _mouse = MakeLocal(ev.Mouse.Where);
                _extent = GetExtent();
                _extent.Grow(1, 1);
                _p = GetPos();
                _s = GetSize() - 1;
                clickPart = GetPartCode();
                switch (clickPart)
                {
                    case sbLeftArrow: // If an arrow was pressed,
                    case sbRightArrow: // do the appropiate action.
                    case sbUpArrow:
                    case sbDownArrow:
                        do
                        {
                            _mouse = MakeLocal(ev.Mouse.Where);
                            if (GetPartCode() == clickPart)
                                SetValue(Value + ScrollStep(clickPart));
                        } while (MouseEvent(ref ev, evMouseAuto));
                        break;
                    default: // Otherwise, move the thumb along the mouse cursor.
                        do
                        {
                            _mouse = MakeLocal(ev.Mouse.Where);
                            i = Size.X == 1 ? _mouse.Y : _mouse.X;
                            i = Math.Max(i, 1); // Keep the thumb between the two arrows.
                            i = Math.Min(i, _s - 1);
                            _p = i;
                            if (_s > 2) // Update the scroll value if there's free scroll space.
                                SetValue((int)(((long)(_p - 1) * (MaxVal - MinVal) + ((_s - 2) >> 1)) / (_s - 2)) + MinVal);
                            DrawPos(_p);
                        } while (MouseEvent(ref ev, evMouseMove));
                        break;
                }
                ClearEvent(ref ev);
                break;
            case evKeyDown:
                if ((State & sfVisible) != 0)
                {
                    clickPart = sbIndicator;
                    if (Size.Y == 1)
                    {
                        switch (CtrlToArrow(ev.KeyDown.KeyCode))
                        {
                            case kbLeft: clickPart = sbLeftArrow; break;
                            case kbRight: clickPart = sbRightArrow; break;
                            case kbCtrlLeft: clickPart = sbPageLeft; break;
                            case kbCtrlRight: clickPart = sbPageRight; break;
                            case kbCtrlUp: clickPart = sbPageUp; break;
                            case kbCtrlDown: clickPart = sbPageDown; break;
                            case kbHome: i = MinVal; break;
                            case kbEnd: i = MaxVal; break;
                            default: return;
                        }
                    }
                    else
                    {
                        switch (CtrlToArrow(ev.KeyDown.KeyCode))
                        {
                            case kbUp: clickPart = sbUpArrow; break;
                            case kbDown: clickPart = sbDownArrow; break;
                            case kbPgUp: clickPart = sbPageUp; break;
                            case kbPgDn: clickPart = sbPageDown; break;
                            case kbCtrlPgUp: i = MinVal; break;
                            case kbCtrlPgDn: i = MaxVal; break;
                            default: return;
                        }
                    }
                    Message(Owner, evBroadcast, cmScrollBarClicked, this); // Clicked
                    if (clickPart != sbIndicator)
                        i = Value + ScrollStep(clickPart);
                    SetValue(i);
                    ClearEvent(ref ev);
                }
                break;
        }
    }

    /// <summary>Broadcasts <c>cmScrollBarChanged</c> to the owner after the value changes.</summary>
    public virtual void ScrollDraw() => Message(Owner, evBroadcast, cmScrollBarChanged, this);

    /// <summary>The signed step for part <paramref name="part"/> (<c>sbXxx</c>).</summary>
    public virtual int ScrollStep(int part)
    {
        var step = (part & 2) == 0 ? ArStep : PgStep;
        return (part & 1) == 0 ? -step : step;
    }

    /// <summary>Sets the value, range and steps, clamping the value; redraws and notifies on change.</summary>
    public void SetParams(int aValue, int aMin, int aMax, int aPgStep, int aArStep)
    {
        aMax = Math.Max(aMax, aMin);
        aValue = Math.Max(aMin, aValue);
        aValue = Math.Min(aMax, aValue);
        var sValue = Value;
        if (sValue != aValue || MinVal != aMin || MaxVal != aMax)
        {
            Value = aValue;
            MinVal = aMin;
            MaxVal = aMax;
            DrawView();
            if (sValue != aValue)
                ScrollDraw();
        }
        PgStep = aPgStep;
        ArStep = aArStep;
    }

    /// <summary>Sets the range.</summary>
    public void SetRange(int aMin, int aMax) => SetParams(Value, aMin, aMax, PgStep, ArStep);

    /// <summary>Sets the page and arrow steps.</summary>
    public void SetStep(int aPgStep, int aArStep) => SetParams(Value, MinVal, MaxVal, aPgStep, aArStep);

    /// <summary>Sets the value.</summary>
    public void SetValue(int aValue) => SetParams(aValue, MinVal, MaxVal, PgStep, ArStep);
}
