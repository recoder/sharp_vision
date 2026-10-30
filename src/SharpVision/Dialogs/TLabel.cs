// Ported from source/tvision/tlabel.cpp, include/tvision/dialogs.h (class TLabel).

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Objects.Util;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Dialogs;

/// <summary>
/// The caption of another control: clicking it or pressing its hotkey focuses the linked
/// control, and it is highlighted while that control has the focus.
/// </summary>
/// <remarks>Palette layout: 1 = normal text, 2 = selected text, 3 = normal shortcut, 4 = selected shortcut.</remarks>
public class TLabel : TStaticText
{
    private static readonly TPalette _palette = new([0x07, 0x08, 0x09, 0x09]);

    /// <summary>The control this label focuses, or null.</summary>
    protected TView? Link;

    /// <summary>Whether the linked control has the focus.</summary>
    protected bool Light;

    /// <summary>Creates a label showing <paramref name="aText"/> (with a <c>~</c> hotkey) for <paramref name="aLink"/>.</summary>
    public TLabel(TRect bounds, string? aText, TView? aLink)
        : base(bounds, aText)
    {
        Link = aLink;
        Options |= ofPreProcess | ofPostProcess;
        EventMask |= evBroadcast;
    }

    /// <inheritdoc />
    public override void ShutDown()
    {
        Link = null;
        base.ShutDown();
    }

    /// <summary>Draws the text on one line, highlighted while the linked control is focused.</summary>
    public override void Draw()
    {
        TAttrPair color;
        var b = new TDrawBuffer();
        int scOff;

        if (Light)
        {
            color = GetColor(0x0402);
            scOff = 0;
        }
        else
        {
            color = GetColor(0x0301);
            scOff = 4;
        }

        b.MoveChar(0, ' ', color, Size.X);
        if (Text is not null)
            b.MoveCStr(1, Text, color);
        if (ShowMarkers)
            b.PutChar(0, SpecialChars[scOff]);
        WriteLine(0, 0, Size.X, 1, b);
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    private void FocusLink(ref TEvent ev)
    {
        if (Link is not null && (Link.Options & ofSelectable) != 0)
            Link.Focus();
        ClearEvent(ref ev);
    }

    /// <summary>Focuses the linked control on a click or the hotkey, and tracks its focus.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evMouseDown)
            FocusLink(ref ev);
        else if (ev.What == evKeyDown)
        {
            var c = HotKey(Text);
            if (ev.KeyDown.KeyCode != 0 &&
                (GetAltCode(c) == ev.KeyDown.KeyCode ||
                 (c != 0 && Owner!.Phase == PhaseType.phPostProcess &&
                  c == char.ToUpperInvariant((char)ev.KeyDown.CharScan.CharCode))))
                FocusLink(ref ev);
        }
        else if (ev.What == evBroadcast && Link is not null &&
                 (ev.Message.Command == cmReceivedFocus || ev.Message.Command == cmReleasedFocus))
        {
            Light = (Link.State & sfFocused) != 0;
            DrawView();
        }
    }
}
