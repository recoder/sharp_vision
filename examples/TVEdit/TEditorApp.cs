using SharpVision.App;
using SharpVision.Dialogs;
using SharpVision.Editors;
using SharpVision.Events;
using SharpVision.Menus;
using SharpVision.Objects;
using SharpVision.StdDlg;
using SharpVision.Views;

using static SharpVision.Dialogs.MsgBox;
using static SharpVision.Editors.EditorCommands;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

/// <summary>A multi-window text editor.</summary>
/// Ported from examples/tvedit/tvedit.h, tvedit1.cpp, tvedit3.cpp (class TEditorApp).
internal sealed class TEditorApp : TApplication
{
    private const ushort cmChangeDrct = 102;

    public TEditorApp(string[] args)
        : base(InitStatusLine, InitMenuBar, InitDeskTop)
    {
        var ts = new TCommandSet();
        ts.EnableCmd(cmSave);
        ts.EnableCmd(cmSaveAs);
        ts.EnableCmd(cmCut);
        ts.EnableCmd(cmCopy);
        ts.EnableCmd(cmPaste);
        ts.EnableCmd(cmClear);
        ts.EnableCmd(cmUndo);
        ts.EnableCmd(cmFind);
        ts.EnableCmd(cmReplace);
        ts.EnableCmd(cmSearchAgain);
        DisableCommands(ts);

        TEditor.EditorDialog = EditDialogs.DoEditDialog;

        foreach (var fileName in args)
            OpenEditor(fileName, true);
        Cascade();
    }

    private TEditWindow? OpenEditor(string? fileName, bool visible)
    {
        var r = DeskTop!.GetExtent();
        var p = ValidView(new TEditWindow(r, fileName ?? "", wnNoNumber));
        if (!visible)
            p?.Hide();
        DeskTop.Insert(p);
        return p as TEditWindow;
    }

    private void FileOpen()
    {
        object?[] fileName = ["*.*"];
        if (EditDialogs.ExecDialog(new TFileDialog("*.*", "Open file", "~N~ame", TFileDialog.fdOpenButton, 100), fileName) != cmCancel)
            OpenEditor((string?)fileName[0], true);
    }

    private void FileNew() => OpenEditor(null, true);

    private void ChangeDir() => EditDialogs.ExecDialog(new TChDirDialog(TChDirDialog.cdNormal, 0), null);

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What != evCommand)
            return;
        switch (ev.Message.Command)
        {
            case cmOpen:
                FileOpen();
                break;
            case cmNew:
                FileNew();
                break;
            case cmChangeDrct:
                ChangeDir();
                break;
            default:
                return;
        }
        ClearEvent(ref ev);
    }

    public static new TMenuBar? InitMenuBar(TRect r)
    {
        var sub1 = new TSubMenu("~F~ile", kbAltF) +
            new TMenuItem("~O~pen", cmOpen, kbF3, hcNoContext, "F3") +
            new TMenuItem("~N~ew", cmNew, kbCtrlN, hcNoContext, "Ctrl-N") +
            new TMenuItem("~S~ave", cmSave, kbF2, hcNoContext, "F2") +
            new TMenuItem("S~a~ve as...", cmSaveAs, kbNoKey) +
            TMenuItem.NewLine() +
            new TMenuItem("~C~hange dir...", cmChangeDrct, kbNoKey) +
            new TMenuItem("~D~OS shell", cmDosShell, kbNoKey) +
            new TMenuItem("E~x~it", cmQuit, kbCtrlQ, hcNoContext, "Ctrl-Q");

        var sub2 = new TSubMenu("~E~dit", kbAltE) +
            new TMenuItem("~U~ndo", cmUndo, kbCtrlU, hcNoContext, "Ctrl-U") +
            TMenuItem.NewLine() +
            new TMenuItem("Cu~t~", cmCut, kbShiftDel, hcNoContext, "Shift-Del") +
            new TMenuItem("~C~opy", cmCopy, kbCtrlIns, hcNoContext, "Ctrl-Ins") +
            new TMenuItem("~P~aste", cmPaste, kbShiftIns, hcNoContext, "Shift-Ins") +
            TMenuItem.NewLine() +
            new TMenuItem("~C~lear", cmClear, kbCtrlDel, hcNoContext, "Ctrl-Del");

        var sub3 = new TSubMenu("~S~earch", kbAltS) +
            new TMenuItem("~F~ind...", cmFind, kbNoKey) +
            new TMenuItem("~R~eplace...", cmReplace, kbNoKey) +
            new TMenuItem("~S~earch again", cmSearchAgain, kbNoKey);

        var sub4 = new TSubMenu("~W~indows", kbAltW) +
            new TMenuItem("~S~ize/move", cmResize, kbCtrlF5, hcNoContext, "Ctrl-F5") +
            new TMenuItem("~Z~oom", cmZoom, kbF5, hcNoContext, "F5") +
            new TMenuItem("~T~ile", cmTile, kbNoKey) +
            new TMenuItem("C~a~scade", cmCascade, kbNoKey) +
            new TMenuItem("~N~ext", cmNext, kbF6, hcNoContext, "F6") +
            new TMenuItem("~P~revious", cmPrev, kbShiftF6, hcNoContext, "Shift-F6") +
            new TMenuItem("~C~lose", cmClose, kbCtrlW, hcNoContext, "Ctrl+W");

        r.B.Y = r.A.Y + 1;
        return new TMenuBar(r, sub1 + sub2 + sub3 + sub4);
    }

    public static new TStatusLine? InitStatusLine(TRect r)
    {
        r.A.Y = r.B.Y - 1;
        return new TStatusLine(r,
            new TStatusDef(0, 0xFFFF) +
                new TStatusItem(null, kbAltX, cmQuit) +
                new TStatusItem("~F2~ Save", kbF2, cmSave) +
                new TStatusItem("~F3~ Open", kbF3, cmOpen) +
                // UPSTREAM: the item says Ctrl-W but is bound to Alt-F3 (Ctrl-W comes from the menu).
                new TStatusItem("~Ctrl-W~ Close", kbAltF3, cmClose) +
                new TStatusItem("~F5~ Zoom", kbF5, cmZoom) +
                new TStatusItem("~F6~ Next", kbF6, cmNext) +
                new TStatusItem("~F10~ Menu", kbF10, cmMenu) +
                new TStatusItem(null, kbShiftDel, cmCut) +
                new TStatusItem(null, kbCtrlIns, cmCopy) +
                new TStatusItem(null, kbShiftIns, cmPaste) +
                new TStatusItem(null, kbCtrlF5, cmResize));
    }

    public override void OutOfMemory() => MessageBox("Not enough memory for this operation.", mfError | mfOKButton);
}
