// Not ported from upstream: a manual test bed for mouse handling in menus, the status line,
// windows and dialog controls. See README.md for the checklist.

using SharpVision.App;
using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Menus;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

var app = new MouseLabApp();
app.Run();
TObject.Destroy(app);

internal sealed class MouseLabApp : TApplication
{
    private const ushort DialogCmd = 100;
    private const ushort WindowCmd = 101;

    // Kept between runs so reopening the dialog shows what OK stored.
    private readonly object?[] _data = ["", "A long line that does not fit, so it scrolls with the mouse"];
    private int _windowCount;

    public MouseLabApp()
        : base(InitStatusLine, InitMenuBar, InitDeskTop)
    {
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What != evCommand)
            return;
        switch (ev.Message.Command)
        {
            case DialogCmd:
                ExecuteDialog(new LabDialog(), _data);
                break;
            case WindowCmd:
                NewWindow();
                break;
            default:
                return;
        }
        ClearEvent(ref ev);
    }

    private void NewWindow()
    {
        var n = ++_windowCount;
        var r = new TRect(2 + n % 10 * 3, 1 + n % 10, 42 + n % 10 * 3, 11 + n % 10);
        var w = new TWindow(r, $"Window {n}", n);
        var text = new TStaticText(new TRect(2, 1, 37, 8),
            "Drag the title bar to move. Drag the bottom-right corner to resize. " +
            "Click [■] to close, [↑] or double-click the title to zoom.");
        text.GrowMode = gfGrowHiX | gfGrowHiY;
        w.Insert(text);
        InsertWindow(w);
    }

    public static new TMenuBar? InitMenuBar(TRect r)
    {
        r.B.Y = r.A.Y + 1;
        return new TMenuBar(r,
            new TSubMenu("~L~ab", kbAltL) +
                new TMenuItem("~D~ialog...", DialogCmd, kbAltD, hcNoContext, "Alt-D") +
                new TMenuItem("~W~indow", WindowCmd, kbAltW, hcNoContext, "Alt-W") +
                TMenuItem.NewLine() +
                new TMenuItem("E~x~it", cmQuit, kbAltX, hcNoContext, "Alt-X") +
            new TSubMenu("~W~indows", kbNoKey) +
                new TMenuItem("~N~ext", cmNext, kbF6, hcNoContext, "F6") +
                new TMenuItem("~Z~oom", cmZoom, kbF5, hcNoContext, "F5") +
                new TMenuItem("~C~lose", cmClose, kbAltF3, hcNoContext, "Alt-F3"));
    }

    public static new TStatusLine? InitStatusLine(TRect r)
    {
        r.A.Y = r.B.Y - 1;
        return new TStatusLine(r,
            new TStatusDef(0, 0xFFFF) +
                new TStatusItem("~Alt-X~ Exit", kbAltX, cmQuit) +
                new TStatusItem("~Alt-D~ Dialog", kbAltD, DialogCmd) +
                new TStatusItem("~Alt-W~ Window", kbAltW, WindowCmd) +
                new TStatusItem(null, kbF10, cmMenu));
    }
}

internal sealed class LabDialog : TDialog
{
    private const ushort AlphaCmd = 201;
    private const ushort BetaCmd = 202;

    private readonly TParamText _status;

    public LabDialog()
        : base(new TRect(0, 0, 50, 15), "Mouse Lab")
    {
        Options |= ofCentered;

        var name = new TInputLine(new TRect(14, 2, 46, 3), 64);
        Insert(name);
        Insert(new TLabel(new TRect(2, 2, 13, 3), "~N~ame", name));

        var narrow = new TInputLine(new TRect(14, 4, 30, 5), 128);
        Insert(narrow);
        Insert(new TLabel(new TRect(2, 4, 13, 5), "~L~ong text", narrow));

        _status = new TParamText(new TRect(2, 6, 48, 7));
        _status.SetText("Last click: none");
        Insert(_status);

        Insert(new TButton(new TRect(2, 8, 14, 10), "~A~lpha", AlphaCmd, bfNormal));
        Insert(new TButton(new TRect(15, 8, 27, 10), "~B~eta", BetaCmd, bfNormal));
        Insert(new TButton(new TRect(24, 12, 34, 14), "O~K~", cmOK, bfDefault));
        Insert(new TButton(new TRect(36, 12, 48, 14), "Cancel", cmCancel, bfNormal));

        SelectNext(false);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evCommand && ev.Message.Command is AlphaCmd or BetaCmd)
        {
            _status.SetText($"Last click: {(ev.Message.Command == AlphaCmd ? "Alpha" : "Beta")}");
            ClearEvent(ref ev);
        }
    }
}
