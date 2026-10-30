// Not ported from upstream: a manual test bed for mouse handling in menus, the status line,
// windows and dialog controls. See README.md for the checklist.

using SharpVision.App;
using SharpVision.Dialogs;
using SharpVision.Drawing;
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
    private const ushort MessageCmd = 102;

    // Kept between runs so reopening the dialog shows what OK stored.
    private readonly object?[] _data = ["", "A long line that does not fit, so it scrolls with the mouse", (ushort)0, (ushort)0,
        new TListBoxRec { Items = ["Apple", "Banana", "Cherry", "Date", "Elderberry", "Fig", "Grape", "Honeydew", "Kiwi", "Lemon", "Mango", "Nectarine"] }];
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
            case MessageCmd:
                var answer = MsgBox.MessageBox("Click one of the buttons below.", MsgBox.mfConfirmation | MsgBox.mfYesNoCancel);
                MsgBox.MessageBox($"You chose command {answer}.", MsgBox.mfInformation | MsgBox.mfOKButton);
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
        InsertWindow(new LabWindow(r, n));
    }

    public static new TMenuBar? InitMenuBar(TRect r)
    {
        r.B.Y = r.A.Y + 1;
        return new TMenuBar(r,
            new TSubMenu("~L~ab", kbAltL) +
                new TMenuItem("~D~ialog...", DialogCmd, kbAltD, hcNoContext, "Alt-D") +
                new TMenuItem("~W~indow", WindowCmd, kbAltW, hcNoContext, "Alt-W") +
                new TMenuItem("~M~essage box...", MessageCmd, kbAltM, hcNoContext, "Alt-M") +
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
        : base(new TRect(0, 0, 70, 19), "Mouse Lab")
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

        var options = new TCheckBoxes(new TRect(2, 9, 24, 12),
            new TSItem("Bold", new TSItem("Italic", new TSItem("Underline", null))));
        Insert(options);
        Insert(new TLabel(new TRect(2, 8, 13, 9), "~O~ptions", options));

        var mode = new TRadioButtons(new TRect(26, 9, 48, 12),
            new TSItem("Insert", new TSItem("Overwrite", new TSItem("Read only", null))));
        Insert(mode);
        Insert(new TLabel(new TRect(26, 8, 37, 9), "~M~ode", mode));

        Insert(new TButton(new TRect(2, 13, 14, 15), "~A~lpha", AlphaCmd, bfNormal));
        Insert(new TButton(new TRect(15, 13, 27, 15), "~B~eta", BetaCmd, bfNormal));
        Insert(new TButton(new TRect(24, 16, 34, 18), "O~K~", cmOK, bfDefault));
        Insert(new TButton(new TRect(36, 16, 48, 18), "Cancel", cmCancel, bfNormal));

        var fruitBar = new TScrollBar(new TRect(66, 2, 67, 15));
        Insert(fruitBar);
        var fruits = new TListBox(new TRect(51, 2, 66, 15), 1, fruitBar);
        Insert(fruits);
        Insert(new TLabel(new TRect(50, 1, 60, 2), "~F~ruits", fruits));

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
        else if (ev.What == evBroadcast && ev.Message.Command == cmListItemSelected && ev.Message.InfoPtr is TListBox box)
            _status.SetText($"Picked: {box.GetText(box.Focused, 255)}");
    }
}

internal sealed class LabWindow : TWindow
{
    public LabWindow(TRect r, int n)
        : base(r, $"Window {n}", n)
    {
        var h = StandardScrollBar(sbHorizontal | sbHandleKeyboard);
        var v = StandardScrollBar(sbVertical | sbHandleKeyboard);
        var bounds = GetExtent();
        bounds.Grow(-1, -1);
        var grid = new LabGrid(bounds, h, v);
        grid.GrowMode = gfGrowHiX | gfGrowHiY;
        Insert(grid);
        grid.SetLimit(LabGrid.Width, LabGrid.Height);
    }
}

// A TScroller whose contents are bigger than the window: help text, then a grid of coordinates.
internal sealed class LabGrid(TRect bounds, TScrollBar h, TScrollBar v) : TScroller(bounds, h, v)
{
    public const int Width = 80;
    public const int Height = 30;

    private static readonly string[] _help =
    [
        "Drag the title bar to move, the bottom-right corner to resize.",
        "Click [■] to close, [↑] or double-click the title to zoom.",
        "Scroll with the bars, the wheel over them, or the arrow keys.",
    ];

    public override void Draw()
    {
        var b = new TDrawBuffer();
        var color = GetColor(1);
        for (var y = 0; y < Size.Y; y++)
        {
            var row = y + Delta.Y;
            var line = row < _help.Length ? _help[row] : "";
            if (row > _help.Length && row < Height)
                for (var col = 0; col < Width; col += 10)
                    line += $"({col},{row})".PadRight(10);
            b.MoveChar(0, ' ', color, Size.X);
            b.MoveStr(0, line, color, Size.X, Delta.X);
            WriteLine(0, y, Size.X, 1, b);
        }
    }
}
