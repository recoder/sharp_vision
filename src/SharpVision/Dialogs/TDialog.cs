using System;

using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Dialogs;

/// <summary>
/// A window for controls: fixed size, gray, closed by Esc, with Enter pressing the default
/// button. In a modal dialog, <c>cmOK</c>, <c>cmCancel</c>, <c>cmYes</c> and <c>cmNo</c> end it.
/// </summary>
/// <remarks>
/// Palette layout: 1-3 = frame, 4-5 = scroll bar, 6 = static text, 7-9 = label,
/// 10-15 = button, 16-18 = cluster, 19-21 = input line, 22-23 = history,
/// 24-25 = history window scroll bar, 26-29 = list viewer, 30 = info pane,
/// 31 = cluster disabled, 32 = reserved.
/// </remarks>
/// Ported from source/tvision/tdialog.cpp, include/tvision/dialogs.h (class TDialog).
public class TDialog : TWindow
{
    private static readonly TPalette _paletteGray = new(
    [
        0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
        0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
    ]);

    private static readonly TPalette _paletteBlue = new(
    [
        0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
        0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
    ]);

    private static readonly TPalette _paletteCyan = new(
    [
        0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
        0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
    ]);

    /// <summary>Creates a dialog with the default frame.</summary>
    public TDialog(TRect bounds, string? aTitle)
        : this(bounds, aTitle, InitFrame)
    {
    }

    /// <summary>
    /// Creates a dialog whose frame is made by <paramref name="createFrame"/>
    /// (upstream's <c>TWindowInit</c>); null for no frame.
    /// </summary>
    public TDialog(TRect bounds, string? aTitle, Func<TRect, TFrame?>? createFrame)
        : base(bounds, aTitle, wnNoNumber, createFrame)
    {
        GrowMode = 0;
        Flags = (byte)(wfMove | wfClose);
        Palette = (short)dpGrayDialog;
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => Palette switch
    {
        (short)dpGrayDialog => _paletteGray,
        (short)dpBlueDialog => _paletteBlue,
        (short)dpCyanDialog => _paletteCyan,
        _ => _paletteGray,
    };

    /// <summary>Turns Esc into <c>cmCancel</c> and Enter into a <c>cmDefault</c> broadcast, and ends a modal dialog.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        switch (ev.What)
        {
            case evKeyDown:
                switch (ev.KeyDown.KeyCode)
                {
                    case kbEsc:
                        ev.What = evCommand;
                        ev.Message.Command = cmCancel;
                        ev.Message.InfoPtr = null;
                        PutEvent(ref ev);
                        ClearEvent(ref ev);
                        break;
                    case kbEnter:
                        ev.What = evBroadcast;
                        ev.Message.Command = cmDefault;
                        ev.Message.InfoPtr = null;
                        PutEvent(ref ev);
                        ClearEvent(ref ev);
                        break;
                }
                break;

            case evCommand:
                switch (ev.Message.Command)
                {
                    case cmOK:
                    case cmCancel:
                    case cmYes:
                    case cmNo:
                        if ((State & sfModal) != 0)
                        {
                            EndModal(ev.Message.Command);
                            ClearEvent(ref ev);
                        }
                        break;
                }
                break;
        }
    }

    /// <summary>A dialog can always be cancelled; other commands ask the subviews.</summary>
    public override bool Valid(ushort command)
    {
        if (command == cmCancel)
            return true;
        return base.Valid(command);
    }
}
