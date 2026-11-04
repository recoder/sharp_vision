using System;
using System.IO;
using SharpVision.App;
using SharpVision.ColorSel;
using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Help;
using SharpVision.Menus;
using SharpVision.Objects;
using SharpVision.Platform;
using SharpVision.StdDlg;
using SharpVision.Streams;
using SharpVision.Views;

using static DemoHelp;
using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Dialogs.MsgBox;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;
using static TVCmds;

/// <summary>The Turbo Vision demo: gadgets, a file viewer, the help system and the color dialog.</summary>
/// Ported from examples/tvdemo/tvdemo.h, tvdemo1.cpp, tvdemo2.cpp, tvdemo3.cpp (class TVDemo).
internal sealed class TVDemo : TApplication
{
    private const string HELP_FILENAME = "demohelp.h32";
    private static bool _helpInUse;

    private readonly THeapView _heap; // Heap view
    private readonly TClockView _clock; // Clock view

    // Constructor for the application. Command line parameters are interpreted
    // as file names and opened. Wildcards are accepted and put up a dialog
    // box with the appropriate search path.
    public TVDemo(string[] args)
        : base(InitStatusLine, InitMenuBar, InitDeskTop)
    {
        var r = GetExtent(); // Create the clock view.
        r.A.X = r.B.X - 9;
        r.B.Y = r.A.Y + 1;
        _clock = new TClockView(r);
        _clock.GrowMode = (byte)(gfGrowLoX | gfGrowHiX);
        Insert(_clock);

        r = GetExtent(); // Create the heap view.
        r.A.X = r.B.X - 13;
        r.A.Y = r.B.Y - 1;
        _heap = new THeapView(r);
        _heap.GrowMode = (byte)gfGrowAll;
        Insert(_heap);

        // shortcut: saving and restoring the desktop needs TStreamable for every view, which is
        // not ported; the menu items stay disabled until it is.
        DisableCommand(cmSaveCmd);
        DisableCommand(cmRestoreCmd);

        foreach (var arg in args) // Display files specified on command line.
        {
            var fileSpec = arg;
            if (fileSpec.EndsWith('\\') || fileSpec.EndsWith('/'))
                fileSpec += "*.*";
            if (fileSpec.Contains('*') || fileSpec.Contains('?'))
                OpenFile(fileSpec);
            else
            {
                var w = ValidView(new TFileWindow(fileSpec));
                if (w is not null)
                    DeskTop!.Insert(w);
            }
        }
    }

    // Event loop to check for context help request.
    public override void GetEvent(ref TEvent ev)
    {
        base.GetEvent(ref ev);
        PrintEvent(ev);
        switch (ev.What)
        {
            case evCommand:
                if (ev.Message.Command == cmHelp && !_helpInUse)
                {
                    _helpInUse = true;
                    // Upstream opens the file in the current directory; it is copied next to the program here.
                    FileStream? helpStrm = null;
                    try
                    {
                        helpStrm = new FileStream(Path.Combine(AppContext.BaseDirectory, HELP_FILENAME), FileMode.Open, FileAccess.ReadWrite);
                    }
                    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                    {
                    }
                    if (helpStrm is null)
                        MessageBox("Could not open help file", mfError | mfOKButton);
                    else
                    {
                        var hFile = new THelpFile(new PStream(helpStrm));
                        TWindow w = new THelpWindow(hFile, GetHelpCtx());
                        if (ValidView(w) is not null)
                        {
                            ExecView(w);
                            Destroy(w);
                        }
                        ClearEvent(ref ev);
                    }
                    _helpInUse = false;
                }
                else if (ev.Message.Command == cmVideoMode)
                {
                    var newMode = TScreen.ScreenMode ^ TDisplay.smFont8x8;
                    SetScreenMode((ushort)newMode);
                }
                break;
            case evMouseDown:
                if (ev.Mouse.Buttons == mbRightButton)
                    ev.What = evNothing;
                break;
        }
    }

    public static new TStatusLine? InitStatusLine(TRect r)
    {
        r.A.Y = r.B.Y - 1;

        return new TStatusLine(r,
            new TStatusDef(0, 50) +
                new TStatusItem("~F1~ Help", kbF1, cmHelp) +
                new TStatusItem("~Alt-X~ Exit", kbAltX, cmQuit) +
                new TStatusItem(null, kbShiftDel, cmCut) +
                new TStatusItem(null, kbCtrlIns, cmCopy) +
                new TStatusItem(null, kbShiftIns, cmPaste) +
                new TStatusItem(null, kbAltF3, cmClose) +
                new TStatusItem(null, kbF10, cmMenu) +
                new TStatusItem(null, kbF5, cmZoom) +
                new TStatusItem(null, kbCtrlF5, cmResize) +
            new TStatusDef(50, 0xffff) +
                new TStatusItem("Howdy", kbF1, cmHelp));
    }

    public static new TMenuBar? InitMenuBar(TRect r)
    {
        // Changing the screen mode only makes sense on DOS, so upstream leaves out "~V~ideo mode" elsewhere.
        var sub1 = new TSubMenu("~≡~", 0, hcSystem) +
            new TMenuItem("~A~bout...", cmAboutCmd, kbNoKey, hcSAbout) +
            TMenuItem.NewLine() +
            new TMenuItem("~P~uzzle", cmPuzzleCmd, kbNoKey, hcSPuzzle) +
            new TMenuItem("Ca~l~endar", cmCalendarCmd, kbNoKey, hcSCalendar) +
            new TMenuItem("Ascii ~T~able", cmAsciiCmd, kbNoKey, hcSAsciiTable) +
            new TMenuItem("~C~alculator", cmCalcCmd, kbNoKey, hcCalculator) +
            new TMenuItem("~E~vent Viewer", cmEventViewCmd, kbAlt0, hcNoContext, "Alt-0");

        var sub2 = new TSubMenu("~F~ile", 0, hcFile) +
            new TMenuItem("~O~pen...", cmOpenCmd, kbF3, hcFOpen, "F3") +
            new TMenuItem("~C~hange Dir...", cmChDirCmd, kbNoKey, hcFChangeDir) +
            TMenuItem.NewLine() +
            new TMenuItem("~D~OS Shell", cmDosShell, kbNoKey, hcFDosShell) +
            new TMenuItem("E~x~it", cmQuit, kbAltX, hcFExit, "Alt-X");

        var sub3 = new TSubMenu("~W~indows", 0, hcWindows) +
            new TMenuItem("~R~esize/move", cmResize, kbCtrlF5, hcWSizeMove, "Ctrl-F5") +
            new TMenuItem("~Z~oom", cmZoom, kbF5, hcWZoom, "F5") +
            new TMenuItem("~N~ext", cmNext, kbF6, hcWNext, "F6") +
            new TMenuItem("~C~lose", cmClose, kbAltF3, hcWClose, "Alt-F3") +
            new TMenuItem("~T~ile", cmTile, kbNoKey, hcWTile) +
            new TMenuItem("C~a~scade", cmCascade, kbNoKey, hcWCascade);

        var sub4 = new TSubMenu("~O~ptions", 0, hcOptions) +
            new TMenuItem("~M~ouse...", cmMouseCmd, kbNoKey, hcOMouse) +
            new TMenuItem("~C~olors...", cmColorCmd, kbNoKey, hcOColors) +
            new TMenuItem("~B~ackground...", cmChBackground, kbNoKey) +
            (TMenuItem)(
                new TSubMenu("~D~esktop", 0) +
                new TMenuItem("~S~ave desktop", cmSaveCmd, kbNoKey, hcOSaveDesktop) +
                new TMenuItem("~R~etrieve desktop", cmRestoreCmd, kbNoKey, hcORestoreDesktop));

        r.B.Y = r.A.Y + 1;
        return new TMenuBar(r, sub1 + sub2 + sub3 + sub4);
    }

    // Event loop to distribute the work.
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == evCommand)
        {
            switch (ev.Message.Command)
            {
                case cmAboutCmd: // About Dialog Box
                    AboutDlgBox();
                    break;
                case cmCalendarCmd: // Calendar Window
                    Calendar();
                    break;
                case cmAsciiCmd: // Ascii Table
                    AsciiTable();
                    break;
                case cmCalcCmd: // Calculator
                    Calculator();
                    break;
                case cmPuzzleCmd: // Puzzle
                    Puzzle();
                    break;
                case cmEventViewCmd: // Open Event Viewer
                    EventViewer();
                    break;
                case cmChBackground: // Change Background pattern
                    ChBackground();
                    break;
                case cmOpenCmd: // View a file
                    OpenFile("*.*");
                    break;
                case cmChDirCmd: // Change directory
                    ChangeDir();
                    break;
                case cmMouseCmd: // Mouse control dialog box
                    Mouse();
                    break;
                case cmColorCmd: // Color control dialog box
                    Colors();
                    break;
                default: // Unknown command
                    return;
            }
            ClearEvent(ref ev);
        }
    }

    private void AboutDlgBox()
    {
        var aboutBox = new TDialog(new TRect(0, 0, 39, 13), "About");

        aboutBox.Insert(new TStaticText(new TRect(9, 2, 30, 9),
            "\u0003Turbo Vision Demo\n\n" + // The \u0003 centers the line.
            "\u0003C++ Version\n\n" +
            "\u0003Copyright (c) 1994\n\n" +
            "\u0003Borland International"));

        aboutBox.Insert(new TButton(new TRect(14, 10, 26, 12), " OK", cmOK, bfDefault));

        aboutBox.Options |= ofCentered;

        ExecuteDialog(aboutBox);
    }

    private void Puzzle()
    {
        if (ValidView(new TPuzzleWindow()) is TPuzzleWindow puzz)
        {
            puzz.HelpCtx = hcPuzzle;
            DeskTop!.Insert(puzz);
        }
    }

    private void AsciiTable()
    {
        if (ValidView(new TAsciiChart()) is TAsciiChart chart)
        {
            chart.HelpCtx = hcAsciiTable;
            DeskTop!.Insert(chart);
        }
    }

    private void ChBackground()
    {
        if (ValidView(new TChBackground(DeskTop!.Background)) is TChBackground b)
        {
            DeskTop.ExecView(b);
            Destroy(b);
        }
    }

    private void Calendar()
    {
        if (ValidView(new TCalendarWindow()) is TCalendarWindow cal)
        {
            cal.HelpCtx = hcCalendar;
            DeskTop!.Insert(cal);
        }
    }

    private void Calculator()
    {
        if (ValidView(new TCalculator()) is TCalculator calc)
        {
            calc.HelpCtx = hcCalculator;
            DeskTop!.Insert(calc);
        }
    }

    private void EventViewer()
    {
        if (Message(DeskTop, evBroadcast, cmFndEventView, null) is TEventViewer viewer)
            viewer.Toggle();
        else
            DeskTop!.Insert(new TEventViewer(DeskTop.GetExtent(), 0x0F00));
    }

    private void PrintEvent(in TEvent ev)
    {
        if (Message(DeskTop, evBroadcast, cmFndEventView, null) is TEventViewer viewer)
            viewer.Print(ev);
    }

    private void ChangeDir()
    {
        var d = ValidView(new TChDirDialog(0, StdDlgCommands.cmChangeDir));

        if (d is not null)
        {
            d.HelpCtx = hcFCChDirDBox;
            DeskTop!.ExecView(d);
            Destroy(d);
        }
    }

    private void Colors()
    {
        var group1 =
            new TColorGroup("Desktop") +
                new TColorItem("Color", 1) +
            new TColorGroup("Menus") +
                new TColorItem("Normal", 2) +
                new TColorItem("Disabled", 3) +
                new TColorItem("Shortcut", 4) +
                new TColorItem("Selected", 5) +
                new TColorItem("Selected disabled", 6) +
                new TColorItem("Shortcut selected", 7);

        var group2 =
            new TColorGroup("Dialogs/Calc") +
                new TColorItem("Frame/background", 33) +
                new TColorItem("Frame icons", 34) +
                new TColorItem("Scroll bar page", 35) +
                new TColorItem("Scroll bar icons", 36) +
                new TColorItem("Static text", 37) +
                new TColorItem("Label normal", 38) +
                new TColorItem("Label selected", 39) +
                new TColorItem("Label shortcut", 40);

        var itemColl1 =
            new TColorItem("Button normal", 41) +
            new TColorItem("Button default", 42) +
            new TColorItem("Button selected", 43) +
            new TColorItem("Button disabled", 44) +
            new TColorItem("Button shortcut", 45) +
            new TColorItem("Button shadow", 46) +
            new TColorItem("Cluster normal", 47) +
            new TColorItem("Cluster selected", 48) +
            new TColorItem("Cluster shortcut", 49);

        var itemColl2 =
            new TColorItem("Input normal", 50) +
            new TColorItem("Input selected", 51) +
            new TColorItem("Input arrow", 52) +
            new TColorItem("History button", 53) +
            new TColorItem("History sides", 54) +
            new TColorItem("History bar page", 55) +
            new TColorItem("History bar icons", 56) +
            new TColorItem("List normal", 57) +
            new TColorItem("List focused", 58) +
            new TColorItem("List selected", 59) +
            new TColorItem("List divider", 60) +
            new TColorItem("Information pane", 61);

        group2 = group2 + itemColl1 + itemColl2;

        var group3 =
            new TColorGroup("Viewer") +
                new TColorItem("Frame passive", 8) +
                new TColorItem("Frame active", 9) +
                new TColorItem("Frame icons", 10) +
                new TColorItem("Scroll bar page", 11) +
                new TColorItem("Scroll bar icons", 12) +
                new TColorItem("Text", 13) +
            new TColorGroup("Puzzle") +
                new TColorItem("Frame passive", 8) +
                new TColorItem("Frame active", 9) +
                new TColorItem("Frame icons", 10) +
                new TColorItem("Scroll bar page", 11) +
                new TColorItem("Scroll bar icons", 12) +
                new TColorItem("Normal text", 13) +
                new TColorItem("Highlighted text", 14);

        var group4 =
            new TColorGroup("Calendar") +
                new TColorItem("Frame passive", 16) +
                new TColorItem("Frame active", 17) +
                new TColorItem("Frame icons", 18) +
                new TColorItem("Scroll bar page", 19) +
                new TColorItem("Scroll bar icons", 20) +
                new TColorItem("Normal text", 21) +
                new TColorItem("Current day", 22) +
            new TColorGroup("Ascii table") +
                new TColorItem("Frame passive", 24) +
                new TColorItem("Frame active", 25) +
                new TColorItem("Frame icons", 26) +
                new TColorItem("Scroll bar page", 27) +
                new TColorItem("Scroll bar icons", 28) +
                new TColorItem("Text", 29);

        var group5 = group1 + group2 + group3 + group4;

        var c = new TColorDialog(null, group5);

        if (ValidView(c) is not null)
        {
            c.HelpCtx = hcOCColorsDBox; // set context help constant
            c.SetData([GetPalette()]);
            if (DeskTop!.ExecView(c) != cmCancel)
            {
                GetPalette().Assign(c.Pal!);
                SetScreenMode(TScreen.ScreenMode);
            }
            Destroy(c);
        }
    }

    private void Mouse()
    {
        var mouseCage = ValidView(new TMouseDialog());

        if (mouseCage is not null)
        {
            // The check box takes a ushort in GetData/SetData.
            object?[] mouseReverse = [(ushort)(TEventQueue.MouseReverse ? 1 : 0)];
            mouseCage.HelpCtx = hcOMMouseDBox;
            mouseCage.SetData(mouseReverse);
            if (DeskTop!.ExecView(mouseCage) != cmCancel)
                mouseCage.GetData(mouseReverse);
            TEventQueue.MouseReverse = (ushort)mouseReverse[0]! != 0;
        }
        Destroy(mouseCage);
    }

    private void OpenFile(string fileSpec)
    {
        var d = ValidView(new TFileDialog(fileSpec, "Open a File", "~N~ame", TFileDialog.fdOpenButton, 100)) as TFileDialog;

        if (d is not null && DeskTop!.ExecView(d) != cmCancel)
        {
            var fileName = d.GetFileName();
            // UPSTREAM: the help context is set after the dialog has run.
            d.HelpCtx = hcFOFileOpenDBox;
            var w = ValidView(new TFileWindow(fileName));
            if (w is not null)
                DeskTop.Insert(w);
        }
        Destroy(d);
    }

    public override void OutOfMemory() =>
        MessageBox("Not enough memory available to complete operation.", mfError | mfOKButton);

    // Updates heap and clock views for this program.
    public override void Idle()
    {
        base.Idle();
        _clock.Update();
        _heap.Update();
        if (DeskTop!.FirstThat(p => (p.Options & ofTileable) != 0) is not null)
        {
            EnableCommand(cmTile);
            EnableCommand(cmCascade);
        }
        else
        {
            DisableCommand(cmTile);
            DisableCommand(cmCascade);
        }
    }
}
