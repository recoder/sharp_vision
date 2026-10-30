using SharpVision.App;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Objects.Util;
using static SharpVision.Views.Commands;

namespace SharpVision.Dialogs;

/// <summary>Modal message and input boxes, run on <see cref="TProgram.Application"/>.</summary>
/// <remarks>Upstream's <c>printf</c>-style overloads are omitted: format the message before passing it.</remarks>
/// Ported from source/tvision/msgbox.cpp, include/tvision/msgbox.h (messageBox, inputBox, mfXxx).
public static class MsgBox
{
    /// <summary>A Warning box.</summary>
    public const ushort mfWarning = 0x0000;

    /// <summary>An Error box.</summary>
    public const ushort mfError = 0x0001;

    /// <summary>An Information box.</summary>
    public const ushort mfInformation = 0x0002;

    /// <summary>A Confirmation box.</summary>
    public const ushort mfConfirmation = 0x0003;

    /// <summary>Adds a Yes button.</summary>
    public const ushort mfYesButton = 0x0100;

    /// <summary>Adds a No button.</summary>
    public const ushort mfNoButton = 0x0200;

    /// <summary>Adds an OK button.</summary>
    public const ushort mfOKButton = 0x0400;

    /// <summary>Adds a Cancel button.</summary>
    public const ushort mfCancelButton = 0x0800;

    /// <summary>Yes, No and Cancel buttons.</summary>
    public const ushort mfYesNoCancel = mfYesButton | mfNoButton | mfCancelButton;

    /// <summary>OK and Cancel buttons.</summary>
    public const ushort mfOKCancel = mfOKButton | mfCancelButton;

    private static readonly ushort[] _commands = [cmYes, cmNo, cmOK, cmCancel];

    private static string ButtonName(int i) => i switch
    {
        0 => MsgBoxText.YesText,
        1 => MsgBoxText.NoText,
        2 => MsgBoxText.OkText,
        _ => MsgBoxText.CancelText,
    };

    private static string Title(int i) => i switch
    {
        0 => MsgBoxText.WarningText,
        1 => MsgBoxText.ErrorText,
        2 => MsgBoxText.InformationText,
        _ => MsgBoxText.ConfirmText,
    };

    /// <summary>Shows <paramref name="msg"/> in a box with bounds <paramref name="r"/> and returns the command of the button pressed.</summary>
    public static ushort MessageBoxRect(TRect r, string msg, ushort aOptions)
    {
        int i, x, buttonCount;
        var buttonList = new TView[5];

        var dialog = new TDialog(r, Title(aOptions & 0x3));

        dialog.Insert(new TStaticText(new TRect(3, 2, dialog.Size.X - 2, dialog.Size.Y - 3), msg));

        for (i = 0, x = -2, buttonCount = 0; i < 4; i++)
        {
            if ((aOptions & (0x0100 << i)) != 0)
            {
                buttonList[buttonCount] = new TButton(new TRect(0, 0, 10, 2), ButtonName(i), _commands[i], bfNormal);
                x += buttonList[buttonCount++].Size.X + 2;
            }
        }

        x = (dialog.Size.X - x) / 2;

        for (i = 0; i < buttonCount; i++)
        {
            dialog.Insert(buttonList[i]);
            buttonList[i].MoveTo(x, dialog.Size.Y - 3);
            x += buttonList[i].Size.X + 2;
        }

        dialog.SelectNext(false);

        var ccode = TProgram.Application!.ExecView(dialog);

        TObject.Destroy(dialog);

        return ccode;
    }

    private static TRect MakeRect(string text)
    {
        var r = new TRect(0, 0, 40, 9);

        var width = Strwidth(text);
        if (width > (r.B.X - 7) * (r.B.Y - 6))
            r.B.Y = width / (r.B.X - 7) + 6 + 1;

        // UPSTREAM: centered with the desktop's size but run on the application, so the box sits
        // one row higher than the desktop's center (the desktop starts below the menu bar).
        r.Move((TProgram.DeskTop!.Size.X - r.B.X) / 2, (TProgram.DeskTop.Size.Y - r.B.Y) / 2);
        return r;
    }

    /// <summary>Shows <paramref name="msg"/> in a box centered on the desktop and returns the command of the button pressed.</summary>
    public static ushort MessageBox(string msg, ushort aOptions) => MessageBoxRect(MakeRect(msg), msg, aOptions);

    /// <summary>Asks for a line of text in a box centered on the desktop. See <see cref="InputBoxRect"/>.</summary>
    public static ushort InputBox(string title, string aLabel, ref string s, byte limit)
    {
        var r = new TRect(0, 0, 60, 8);
        r.Move((TProgram.DeskTop!.Size.X - r.B.X) / 2, (TProgram.DeskTop.Size.Y - r.B.Y) / 2);
        return InputBoxRect(r, title, aLabel, ref s, limit);
    }

    /// <summary>
    /// Asks for a line of text, starting from <paramref name="s"/>, in a box with bounds
    /// <paramref name="bounds"/>. Unless cancelled, stores the result in <paramref name="s"/>.
    /// </summary>
    /// <remarks>The label width is measured in code units, where upstream counts bytes.</remarks>
    public static ushort InputBoxRect(TRect bounds, string title, string aLabel, ref string s, byte limit)
    {
        var dialog = new TDialog(bounds, title);

        var r = new TRect(4 + aLabel.Length, 2, dialog.Size.X - 3, 3);
        var control = new TInputLine(r, limit);
        dialog.Insert(control);

        r = new TRect(2, 2, 3 + aLabel.Length, 3);
        dialog.Insert(new TLabel(r, aLabel, control));

        r = new TRect(dialog.Size.X - 24, dialog.Size.Y - 4, dialog.Size.X - 14, dialog.Size.Y - 2);
        dialog.Insert(new TButton(r, MsgBoxText.OkText, cmOK, bfDefault));

        r.A.X += 12;
        r.B.X += 12;
        dialog.Insert(new TButton(r, MsgBoxText.CancelText, cmCancel, bfNormal));

        dialog.SelectNext(false);
        var data = new object?[] { s };
        dialog.SetData(data);
        var c = TProgram.Application!.ExecView(dialog);
        if (c != cmCancel)
        {
            dialog.GetData(data);
            s = (string)data[0]!;
        }
        TObject.Destroy(dialog);
        return c;
    }
}
