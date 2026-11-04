using SharpVision.App;
using SharpVision.Dialogs;
using SharpVision.Editors;
using SharpVision.Objects;
using SharpVision.StdDlg;
using SharpVision.Views;

using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Dialogs.MsgBox;
using static SharpVision.Editors.EditorCommands;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

/// <summary>The editor's dialogs and its <see cref="TEditor.EditorDialog"/> handler.</summary>
/// Ported from examples/tvedit/tvedit2.cpp, tvedit3.cpp (execDialog, createFindDialog, createReplaceDialog, doEditDialog).
internal static class EditDialogs
{
    public static ushort ExecDialog(TDialog d, object?[]? data)
    {
        var p = TProgram.Application!.ValidView(d);
        if (p is null)
            return cmCancel;
        if (data is not null)
            p.SetData(data);
        var result = TProgram.DeskTop!.ExecView(p);
        if (result != cmCancel && data is not null)
            p.GetData(data);
        TObject.Destroy(p);
        return result;
    }

    public static TDialog CreateFindDialog()
    {
        var d = new TDialog(new TRect(0, 0, 38, 12), "Find");
        d.Options |= ofCentered;

        var control = new TInputLine(new TRect(3, 3, 32, 4), 80);
        d.Insert(control);
        d.Insert(new TLabel(new TRect(2, 2, 15, 3), "~T~ext to find", control));
        d.Insert(new THistory(new TRect(32, 3, 35, 4), control, 10));

        d.Insert(new TCheckBoxes(new TRect(3, 5, 35, 7),
            new TSItem("~C~ase sensitive",
            new TSItem("~W~hole words only", null))));

        d.Insert(new TButton(new TRect(14, 9, 24, 11), "O~K~", cmOK, bfDefault));
        d.Insert(new TButton(new TRect(26, 9, 36, 11), "Cancel", cmCancel, bfNormal));

        d.SelectNext(false);
        return d;
    }

    public static TDialog CreateReplaceDialog()
    {
        var d = new TDialog(new TRect(0, 0, 40, 16), "Replace");
        d.Options |= ofCentered;

        var control = new TInputLine(new TRect(3, 3, 34, 4), 80);
        d.Insert(control);
        d.Insert(new TLabel(new TRect(2, 2, 15, 3), "~T~ext to find", control));
        d.Insert(new THistory(new TRect(34, 3, 37, 4), control, 10));

        control = new TInputLine(new TRect(3, 6, 34, 7), 80);
        d.Insert(control);
        d.Insert(new TLabel(new TRect(2, 5, 12, 6), "~N~ew text", control));
        d.Insert(new THistory(new TRect(34, 6, 37, 7), control, 11));

        d.Insert(new TCheckBoxes(new TRect(3, 8, 37, 12),
            new TSItem("~C~ase sensitive",
            new TSItem("~W~hole words only",
            new TSItem("~P~rompt on replace",
            new TSItem("~R~eplace all", null))))));

        d.Insert(new TButton(new TRect(17, 13, 27, 15), "O~K~", cmOK, bfDefault));
        d.Insert(new TButton(new TRect(28, 13, 38, 15), "Cancel", cmCancel, bfNormal));

        d.SelectNext(false);
        return d;
    }

    public static ushort DoEditDialog(int dialog, params object?[] args)
    {
        switch (dialog)
        {
            case edOutOfMemory:
                return MessageBox("Not enough memory for this operation", mfError | mfOKButton);
            case edReadError:
                return MessageBox($"Error reading file {args[0]}.", mfError | mfOKButton);
            case edWriteError:
                return MessageBox($"Error writing file {args[0]}.", mfError | mfOKButton);
            case edCreateError:
                return MessageBox($"Error creating file {args[0]}.", mfError | mfOKButton);
            case edSaveModify:
                return MessageBox($"{args[0]} has been modified. Save?", mfInformation | mfYesNoCancel);
            case edSaveUntitled:
                return MessageBox("Save untitled file?", mfInformation | mfYesNoCancel);
            case edSaveAs:
                return ExecDialog(new TFileDialog("*.*", "Save file as", "~N~ame", TFileDialog.fdOKButton, 101), (object?[])args[0]!);
            case edFind:
                {
                    // The C++ record has the dialog's data layout; here it is copied to and from the slots.
                    var rec = (TFindDialogRec)args[0]!;
                    object?[] data = [rec.Find, rec.Options];
                    var result = ExecDialog(CreateFindDialog(), data);
                    (rec.Find, rec.Options) = ((string)data[0]!, (ushort)data[1]!);
                    return result;
                }
            case edSearchFailed:
                return MessageBox("Search string not found.", mfError | mfOKButton);
            case edReplace:
                {
                    var rec = (TReplaceDialogRec)args[0]!;
                    object?[] data = [rec.Find, rec.Replace, rec.Options];
                    var result = ExecDialog(CreateReplaceDialog(), data);
                    (rec.Find, rec.Replace, rec.Options) = ((string)data[0]!, (string)data[1]!, (ushort)data[2]!);
                    return result;
                }
            case edReplacePrompt:
                {
                    // Avoid placing the dialog on the same line as the cursor
                    var deskTop = TProgram.DeskTop!;
                    var r = new TRect(0, 1, 40, 8);
                    r.Move((deskTop.Size.X - r.B.X) / 2, 0);
                    var t = deskTop.MakeGlobal(r.B);
                    t.Y++;
                    var pt = (TPoint)args[0]!;
                    if (pt.Y <= t.Y)
                        r.Move(0, deskTop.Size.Y - r.B.Y - 2);
                    return MessageBoxRect(r, "Replace this occurence?", mfYesNoCancel | mfInformation);
                }
        }
        return cmCancel;
    }
}
