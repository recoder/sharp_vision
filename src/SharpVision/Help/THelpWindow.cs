using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Views.ViewFlags;

namespace SharpVision.Help;

/// <summary>A centered window holding a <see cref="THelpViewer"/>.</summary>
/// Ported from source/tvision/help.cpp, source/tvision/tvtext2.cpp, include/tvision/help.h (class THelpWindow).
public class THelpWindow : TWindow
{
    private static readonly TPalette _palette = new([0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87]);

    /// <summary>The window title.</summary>
    public static string HelpWinTitle = "Help";

    /// <summary>Creates the window showing the topic of <paramref name="context"/> in <paramref name="hFile"/>.</summary>
    public THelpWindow(THelpFile hFile, ushort context) : base(new TRect(0, 0, 50, 18), HelpWinTitle, wnNoNumber)
    {
        var r = new TRect(0, 0, 50, 18);
        Options |= ofCentered;
        r.Grow(-2, -1);
        Insert(new THelpViewer(r,
            StandardScrollBar(sbHorizontal | sbHandleKeyboard),
            StandardScrollBar(sbVertical | sbHandleKeyboard), hFile, context));
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;
}
