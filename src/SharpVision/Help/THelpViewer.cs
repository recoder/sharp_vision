using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Help;

/// <summary>Shows a help topic with its cross-references highlighted; Tab, Enter and clicks follow them.</summary>
/// Ported from source/tvision/help.cpp, include/tvision/help.h (class THelpViewer).
public class THelpViewer : TScroller
{
    private static readonly TPalette _palette = new([0x06, 0x07, 0x08]);

    /// <summary>The help file; shut down with the viewer.</summary>
    public THelpFile HFile;

    /// <summary>The topic shown.</summary>
    public THelpTopic Topic;

    /// <summary>The 1-based number of the selected cross-reference.</summary>
    public int Selected;

    /// <summary>Creates a viewer showing the topic of <paramref name="context"/> in <paramref name="aHelpFile"/>.</summary>
    public THelpViewer(TRect bounds, TScrollBar? aHScrollBar, TScrollBar? aVScrollBar, THelpFile aHelpFile, ushort context)
        : base(bounds, aHScrollBar, aVScrollBar)
    {
        Options |= ofSelectable;
        GrowMode = gfGrowHiX | gfGrowHiY;
        HFile = aHelpFile;
        Topic = aHelpFile.GetTopic(context);
        Topic.SetWidth(Size.X); // Width used for line wrapping.
        SetLimit(Topic.LongestLineWidth(), Topic.NumLines());
        Selected = 1;
    }

    /// <summary>Shuts down the help file.</summary>
    public override void ShutDown()
    {
        Destroy(HFile);
        base.ShutDown();
    }

    /// <summary>Rewraps the topic to the new width.</summary>
    public override void ChangeBounds(TRect bounds)
    {
        base.ChangeBounds(bounds);
        Topic.SetWidth(Size.X);
        SetLimit(Topic.LongestLineWidth(), Topic.NumLines());
    }

    /// <summary>Draws the visible lines and highlights the cross-references.</summary>
    public override void Draw()
    {
        var b = new TDrawBuffer();
        TColorAttr normal = GetColor(1);
        TColorAttr keyword = GetColor(2);
        TColorAttr selKeyword = GetColor(3);
        var keyCount = 0;
        var keyPoint = new TPoint(0, 0);
        byte keyLength = 0;
        Topic.SetWidth(Size.X);
        if (Topic.GetNumCrossRefs() > 0)
        {
            do
            {
                Topic.GetCrossRef(keyCount, out keyPoint, out keyLength, out _);
                ++keyCount;
            } while (keyCount < Topic.GetNumCrossRefs() && keyPoint.Y <= Delta.Y);
        }
        for (var i = 1; i <= Size.Y; ++i)
        {
            b.MoveChar(0, ' ', normal, Size.X);
            var line = Topic.GetLine(i + Delta.Y);
            if (TText.Width(line) > Delta.X)
                b.MoveStr(0, line, normal, Size.X, Delta.X);
            else
                b.MoveStr(0, "", normal);
            while (i + Delta.Y == keyPoint.Y)
            {
                int l = keyLength;
                if (keyPoint.X < Delta.X)
                {
                    l -= Delta.X - keyPoint.X;
                    keyPoint.X = Delta.X;
                }
                var c = keyCount == Selected ? selKeyword : keyword;
                for (var j = 0; j < l; ++j)
                    b.PutAttribute(keyPoint.X - Delta.X + j, c);
                if (keyCount < Topic.GetNumCrossRefs())
                {
                    Topic.GetCrossRef(keyCount, out keyPoint, out keyLength, out _);
                    keyCount++;
                }
                else
                    keyPoint.Y = 0;
            }
            WriteLine(0, i - 1, Size.X, 1, b);
        }
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    /// <summary>Scrolls so that cross-reference <paramref name="selected"/> (0-based) is visible.</summary>
    public void MakeSelectVisible(int selected, out TPoint keyPoint, out byte keyLength, out int keyRef)
    {
        Topic.GetCrossRef(selected, out keyPoint, out keyLength, out keyRef);
        var d = Delta;
        if (keyPoint.X < d.X)
            d.X = keyPoint.X;
        if (keyPoint.X > d.X + Size.X)
            d.X = keyPoint.X - Size.X;
        if (keyPoint.Y <= d.Y)
            d.Y = keyPoint.Y - 1;
        if (keyPoint.Y > d.Y + Size.Y)
            d.Y = keyPoint.Y - Size.Y;
        if (d.X != Delta.X || d.Y != Delta.Y)
            ScrollTo(d.X, d.Y);
    }

    /// <summary>Shows the topic of context <paramref name="keyRef"/>.</summary>
    public void SwitchToTopic(int keyRef)
    {
        Topic = HFile.GetTopic(keyRef);
        Topic.SetWidth(Size.X);
        ScrollTo(0, 0);
        SetLimit(Topic.LongestLineWidth(), Topic.NumLines());
        Selected = 1;
        DrawView();
    }

    /// <summary>Tab and Shift-Tab select cross-references, Enter or a click follows one, Esc closes.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        TPoint keyPoint;
        int keyRef;

        base.HandleEvent(ref ev);
        switch (ev.What)
        {
            case evKeyDown:
                switch (ev.KeyDown.KeyCode)
                {
                    case kbTab:
                        ++Selected;
                        if (Selected > Topic.GetNumCrossRefs())
                            Selected = 1;
                        if (Topic.GetNumCrossRefs() != 0)
                            MakeSelectVisible(Selected - 1, out _, out _, out _);
                        break;
                    case kbShiftTab:
                        --Selected;
                        if (Selected == 0)
                            Selected = Topic.GetNumCrossRefs();
                        if (Topic.GetNumCrossRefs() != 0)
                            MakeSelectVisible(Selected - 1, out _, out _, out _);
                        break;
                    case kbEnter:
                        if (Selected <= Topic.GetNumCrossRefs())
                        {
                            Topic.GetCrossRef(Selected - 1, out _, out _, out keyRef);
                            SwitchToTopic(keyRef);
                        }
                        break;
                    case kbEsc:
                        ev.What = evCommand;
                        ev.Message.Command = cmClose;
                        PutEvent(ref ev);
                        break;
                    default:
                        return;
                }
                DrawView();
                ClearEvent(ref ev);
                break;

            case evMouseDown:
                var mouse = MakeLocal(ev.Mouse.Where);
                mouse.X += Delta.X;
                mouse.Y += Delta.Y;
                var keyCount = 0;
                byte keyLength;
                do
                {
                    ++keyCount;
                    if (keyCount > Topic.GetNumCrossRefs())
                        return;
                    Topic.GetCrossRef(keyCount - 1, out keyPoint, out keyLength, out keyRef);
                } while (!(keyPoint.Y == mouse.Y + 1 && mouse.X >= keyPoint.X && mouse.X < keyPoint.X + keyLength));
                Selected = keyCount;
                DrawView();
                SwitchToTopic(keyRef);
                ClearEvent(ref ev);
                break;

            case evCommand:
                if (ev.Message.Command == cmClose && (Owner!.State & sfModal) != 0)
                {
                    EndModal(cmClose);
                    ClearEvent(ref ev);
                }
                break;
        }
    }
}
