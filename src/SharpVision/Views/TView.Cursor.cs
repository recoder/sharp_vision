// Ported from source/tvision/tvcursor.cpp (TView::resetCursor).

using SharpVision.Platform;

using static SharpVision.Views.ViewFlags;

namespace SharpVision.Views;

public partial class TView
{
    /// <summary>
    /// Updates the hardware caret: shown at the view's cursor if the view is visible,
    /// focused, has <c>sfCursorVis</c>, and the position is not covered by other views.
    /// </summary>
    public virtual void ResetCursor()
    {
        int x = Cursor.X, y = Cursor.Y;
        var caretSize = ComputeCaretSize(this, ref x, ref y);
        if (caretSize != 0)
            THardwareInfo.SetCaretPosition(x, y);
        THardwareInfo.SetCaretSize(caretSize);
    }

    private static int DecideCaretSize(TView p)
    {
        if ((p.State & sfCursorIns) != 0)
            return 100;
        return TScreen.CursorLines & 0xFF;
    }

    // Pre: p.Owner is not null.
    private static bool CaretIsCoveredBySiblings(TView p, int x, int y)
    {
        for (var u = p.Owner!.Last!.Next!; u != p; u = u.Next!)
        {
            if ((u.State & sfVisible) != 0 &&
                u.Origin.Y <= y && y < u.Origin.Y + u.Size.Y &&
                u.Origin.X <= x && x < u.Origin.X + u.Size.X)
                return true;
        }
        return false;
    }

    private static int ComputeCaretSize(TView p, ref int x, ref int y)
    {
        const ushort mask = sfVisible | sfCursorVis | sfFocused;
        if ((p.State & mask) != mask)
            return 0;

        var v = p;
        while (0 <= y && y < v.Size.Y && 0 <= x && x < v.Size.X)
        {
            y += v.Origin.Y;
            x += v.Origin.X;

            if (v.Owner is null)
                return DecideCaretSize(p);

            if ((v.Owner.State & sfVisible) == 0 || CaretIsCoveredBySiblings(v, x, y))
                break;

            v = v.Owner;
        }
        return 0;
    }
}
