// Ported from include/tvision/app.h (TDeskInit, TDeskTop), source/tvision/tdesktop.cpp and
// source/tvision/tvtext2.cpp (defaultBkgrnd).

using System;

using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.App;

/// <summary>The area between the menu bar and the status line that holds the windows.</summary>
public class TDeskTop : TGroup
{
    /// <summary>The default background pattern.</summary>
    public const char DefaultBkgrnd = '░'; // '\xB0' in CP437.

    /// <summary>The background view, or null.</summary>
    public TBackground? Background;

    /// <summary>Whether <see cref="Tile"/> fills columns before rows.</summary>
    protected bool TileColumnsFirst;

    /// <summary>Creates a desktop with the default background.</summary>
    public TDeskTop(TRect bounds)
        : this(bounds, InitBackground)
    {
    }

    /// <summary>
    /// Creates a desktop whose background is made by <paramref name="createBackground"/>
    /// (upstream's <c>TDeskInit</c>); null for no background.
    /// </summary>
    public TDeskTop(TRect bounds, Func<TRect, TBackground?>? createBackground)
        : base(bounds)
    {
        GrowMode = (byte)(gfGrowHiX | gfGrowHiY);
        if (createBackground is not null && (Background = createBackground(GetExtent())) is not null)
            Insert(Background);
    }

    /// <inheritdoc />
    public override void ShutDown()
    {
        Background = null;
        base.ShutDown();
    }

    private static bool Tileable(TView p) =>
        (p.Options & ofTileable) != 0 && (p.State & sfVisible) != 0;

    /// <summary>Arranges the tileable windows in a staircase within <paramref name="r"/>.</summary>
    public void Cascade(TRect r)
    {
        var cascadeNum = 0;
        TView? lastView = null;
        ForEach(p =>
        {
            if (Tileable(p))
            {
                cascadeNum++;
                lastView = p;
            }
        });
        if (cascadeNum > 0)
        {
            lastView!.SizeLimits(out var min, out _);
            if (min.X > r.B.X - r.A.X - cascadeNum || min.Y > r.B.Y - r.A.Y - cascadeNum)
                TileError();
            else
            {
                cascadeNum--;
                Lock();
                ForEach(p =>
                {
                    if (Tileable(p) && cascadeNum >= 0)
                    {
                        var nr = r;
                        nr.A.X += cascadeNum;
                        nr.A.Y += cascadeNum;
                        p.Locate(ref nr);
                        cascadeNum--;
                    }
                });
                Unlock();
            }
        }
    }

    /// <summary>Handles <c>cmNext</c> and <c>cmPrev</c> (cycling through the windows).</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evCommand)
        {
            switch (ev.Message.Command)
            {
                case cmNext:
                    if (Valid(cmReleasedFocus))
                        SelectNext(false);
                    break;
                case cmPrev:
                    if (Valid(cmReleasedFocus))
                        Current!.PutInFrontOf(Background);
                    break;
                default:
                    return;
            }
            ClearEvent(ref ev);
        }
    }

    /// <summary>Creates the default background.</summary>
    public static TBackground? InitBackground(TRect r) => new(r, DefaultBkgrnd);

    private static short ISqr(short i)
    {
        short res1 = 2;
        var res2 = (short)(i / res1);
        while (Math.Abs(res1 - res2) > 1)
        {
            res1 = (short)((res1 + res2) / 2);
            res2 = (short)(i / res1);
        }
        return res1 < res2 ? res1 : res2;
    }

    private static void MostEqualDivisors(short n, out short x, out short y, bool favorY)
    {
        var i = ISqr(n);
        if (n % i != 0 && n % (i + 1) == 0)
            i++;
        if (i < n / i)
            i = (short)(n / i);
        if (favorY)
        {
            x = (short)(n / i);
            y = i;
        }
        else
        {
            y = (short)(n / i);
            x = i;
        }
    }

    private static int DividerLoc(int lo, int hi, int num, int pos) =>
        (int)((long)(hi - lo) * pos / num + lo);

    private static TRect CalcTileRect(int pos, TRect r, int numCols, int numRows, int leftOver)
    {
        int x, y;
        var nRect = new TRect();
        var d = (numCols - leftOver) * numRows;
        if (pos < d)
        {
            x = pos / numRows;
            y = pos % numRows;
        }
        else
        {
            x = (pos - d) / (numRows + 1) + (numCols - leftOver);
            y = (pos - d) % (numRows + 1);
        }
        nRect.A.X = DividerLoc(r.A.X, r.B.X, numCols, x);
        nRect.B.X = DividerLoc(r.A.X, r.B.X, numCols, x + 1);
        if (pos >= d)
        {
            nRect.A.Y = DividerLoc(r.A.Y, r.B.Y, numRows + 1, y);
            nRect.B.Y = DividerLoc(r.A.Y, r.B.Y, numRows + 1, y + 1);
        }
        else
        {
            nRect.A.Y = DividerLoc(r.A.Y, r.B.Y, numRows, y);
            nRect.B.Y = DividerLoc(r.A.Y, r.B.Y, numRows, y + 1);
        }
        return nRect;
    }

    /// <summary>Arranges the tileable windows side by side within <paramref name="r"/>.</summary>
    public void Tile(TRect r)
    {
        short numTileable = 0;
        ForEach(p =>
        {
            if (Tileable(p))
                numTileable++;
        });
        if (numTileable > 0)
        {
            MostEqualDivisors(numTileable, out var numCols, out var numRows, !TileColumnsFirst);
            if ((r.B.X - r.A.X) / numCols == 0 || (r.B.Y - r.A.Y) / numRows == 0)
                TileError();
            else
            {
                var leftOver = numTileable % numCols;
                var tileNum = numTileable - 1;
                Lock();
                ForEach(p =>
                {
                    if (Tileable(p))
                    {
                        var nr = CalcTileRect(tileNum, r, numCols, numRows, leftOver);
                        p.Locate(ref nr);
                        tileNum--;
                    }
                });
                Unlock();
            }
        }
    }

    /// <summary>Called when the windows do not fit for <see cref="Tile"/> or <see cref="Cascade"/>. Does nothing by default.</summary>
    public virtual void TileError()
    {
    }
}
