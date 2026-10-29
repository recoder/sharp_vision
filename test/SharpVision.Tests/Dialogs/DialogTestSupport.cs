using SharpVision.Drawing;
using SharpVision.Tests.Views;
using SharpVision.Views;

using static SharpVision.Views.ViewFlags;

namespace SharpVision.Tests.Dialogs;

/// <summary>
/// Stands in for the application palette: entry n maps to BIOS attribute n, so a view's
/// drawn attribute is the application palette index it resolved to. Hands out timer ids.
/// </summary>
internal sealed class DialogDesk : TGroup
{
    public DialogDesk(TestScreen screen)
        : base(screen.GetExtent())
    {
        screen.Insert(this);
        // Like TDeskTop's background: FirstMatch starts at Last.
        Insert(new TView(GetExtent()));
    }

    public object? LastTimer { get; private set; }

    public override TPalette GetPalette()
    {
        var entries = new byte[0x7F];
        for (var i = 0; i < entries.Length; i++)
            entries[i] = (byte)(i + 1);
        return new TPalette(entries);
    }

    public override object? SetTimer(uint timeoutMs, int periodMs = -1) => LastTimer = new object();

    public static (TestScreen Screen, DialogDesk Desk) Create(int w = 40, int h = 15)
    {
        // Focused like TProgram, so the selected control in the dialog gets sfFocused.
        var screen = new TestScreen(w, h);
        screen.State |= sfFocused;
        return (screen, new DialogDesk(screen));
    }
}
