using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Events.EventCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

/// <summary>Sets the double click speed and swaps the mouse buttons.</summary>
/// Ported from examples/tvdemo/mousedlg.h, mousedlg.cpp (class TMouseDialog).
internal sealed class TMouseDialog : TDialog
{
    private readonly TScrollBar _mouseScrollBar;
    private readonly ushort _oldDelay;

    public TMouseDialog() : base(new TRect(0, 0, 34, 12), "Mouse options")
    {
        var r = new TRect(3, 4, 30, 5);

        Options |= ofCentered;

        _mouseScrollBar = new TScrollBar(r);
        _mouseScrollBar.SetParams(1, 1, 20, 20, 1);
        _mouseScrollBar.Options |= ofSelectable;
        _mouseScrollBar.SetValue(TEventQueue.DoubleDelay);
        Insert(_mouseScrollBar);

        r = new TRect(2, 2, 21, 3);
        Insert(new TLabel(r, "~M~ouse double click", _mouseScrollBar));

        r = new TRect(3, 3, 30, 4);
        Insert(new TClickTester(r, "Fast       Medium      Slow"));

        r = new TRect(3, 6, 30, 7);
        Insert(new TCheckBoxes(r, new TSItem("~R~everse mouse buttons", null)));
        _oldDelay = TEventQueue.DoubleDelay;

        r = new TRect(9, 9, 19, 11);
        Insert(new TButton(r, "O~K~", cmOK, bfDefault));

        r = new TRect(21, 9, 31, 11);
        Insert(new TButton(r, "Cancel", cmCancel, bfNormal));

        SelectNext(false);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        switch (ev.What)
        {
            case evCommand:
                if (ev.Message.Command == cmCancel)
                    TEventQueue.DoubleDelay = _oldDelay;
                break;
            case evBroadcast:
                if (ev.Message.Command == cmScrollBarChanged)
                {
                    TEventQueue.DoubleDelay = (ushort)_mouseScrollBar.Value;
                    ClearEvent(ref ev);
                }
                break;
        }
    }
}
