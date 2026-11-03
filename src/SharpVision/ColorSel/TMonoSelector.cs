using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;

using static SharpVision.ColorSel.ColorSelCommands;
using static SharpVision.Events.EventCodes;

namespace SharpVision.ColorSel;

/// <summary>Radio buttons for the four monochrome attributes, shown instead of the color selectors on monochrome screens.</summary>
/// Ported from source/tvision/colorsel.cpp, source/tvision/tvtext1.cpp, include/tvision/colorsel.h (class TMonoSelector).
public class TMonoSelector : TCluster
{
    private static readonly byte[] _monoColors = [0x07, 0x0F, 0x01, 0x70, 0x09];

    /// <summary>The radio-button icon.</summary>
    public static string Button = " ( ) ";
    /// <summary>The label of the normal attribute.</summary>
    public static string Normal = "Normal";
    /// <summary>The label of the highlight attribute.</summary>
    public static string Highlight = "Highlight";
    /// <summary>The label of the underline attribute.</summary>
    public static string Underline = "Underline";
    /// <summary>The label of the inverse attribute.</summary>
    public static string Inverse = "Inverse";

    /// <summary>Creates the selector.</summary>
    public TMonoSelector(TRect bounds)
        : base(bounds, new TSItem(Normal, new TSItem(Highlight, new TSItem(Underline, new TSItem(Inverse, null)))))
    {
        EventMask |= evBroadcast;
    }

    /// <summary>Draws the items as radio buttons.</summary>
    public override void Draw() => DrawBox(Button, '•');

    /// <summary>Follows <c>cmColorSet</c>.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evBroadcast && ev.Message.Command == cmColorSet)
        {
            Value = ev.Message.InfoByte;
            DrawView();
        }
    }

    /// <summary>Whether <paramref name="item"/>'s attribute is the current value.</summary>
    public override bool Mark(int item) => _monoColors[item] == Value;

    /// <summary>Broadcasts the current value as a foreground and a background change.</summary>
    public void NewColor()
    {
        Message(Owner, evBroadcast, cmColorForegroundChanged, (int)(Value & 0x0F));
        Message(Owner, evBroadcast, cmColorBackgroundChanged, (int)((Value >> 4) & 0x0F));
    }

    /// <summary>Takes <paramref name="item"/>'s attribute.</summary>
    public override void Press(int item)
    {
        Value = _monoColors[item];
        NewColor();
    }

    /// <summary>Takes <paramref name="item"/>'s attribute.</summary>
    public override void MovedTo(int item)
    {
        Value = _monoColors[item];
        NewColor();
    }
}
