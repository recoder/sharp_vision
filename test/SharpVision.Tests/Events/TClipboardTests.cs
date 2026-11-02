using SharpVision.Dialogs;
using SharpVision.Editors;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Tests.Dialogs;
using SharpVision.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.Commands;

namespace SharpVision.Tests.Events;

// Headless, so THardwareInfo has no system clipboard and the local one is used.
public sealed class TClipboardTests : CommandStateTest
{
    // Feeds the pending paste events to the view.
    private static void Pump(TView view)
    {
        while (true)
        {
            var ev = default(TEvent);
            TEventQueue.GetKeyEvent(ref ev);
            if (ev.What == evNothing)
                return;
            view.HandleEvent(ref ev);
        }
    }

    // Selected and focused, as the views handle commands and keys only then.
    private static T Focused<T>(T view)
        where T : TView
    {
        var (_, desk) = DialogDesk.Create(30, 8);
        desk.Insert(view);
        return view;
    }

    private static void Command(TView view, ushort command)
    {
        var ev = new TEvent { What = evCommand, Message = new MessageEvent { Command = command } };
        view.HandleEvent(ref ev);
        Assert.Equal(evNothing, ev.What);
    }

    [Fact]
    public void Should_PasteSetText_As_PasteKeyEvents()
    {
        TClipboard.SetText("a\r\nb");
        TClipboard.RequestText();

        var text = "";
        var ev = default(TEvent);
        for (TEventQueue.GetKeyEvent(ref ev); ev.What != evNothing; TEventQueue.GetKeyEvent(ref ev))
        {
            Assert.NotEqual(0, ev.KeyDown.ControlKeyState & KeyCodes.kbPaste);
            text += ev.KeyDown.Text;
        }

        Assert.Equal("a\nb", text);
    }

    [Fact]
    public void Should_CutAndPaste_In_InputLine()
    {
        var line = Focused(new TInputLine(new TRect(0, 0, 10, 1), 20) { Data = "hello" });
        line.SelectAll(true);

        Command(line, cmCut);
        Assert.Equal("", line.Data);

        Command(line, cmPaste);
        Pump(line);
        Command(line, cmPaste);
        Pump(line);
        Assert.Equal("hellohello", line.Data);
    }

    [Fact]
    public void Should_CopyAndPaste_In_Editor_Without_ClipboardEditor()
    {
        var e = Focused(new TEditor(new TRect(0, 0, 20, 5), null, null, null, 64));
        e.InsertText("one two", false);
        e.SetSelect(0, 3, true);

        Assert.True(e.ClipCopy());
        e.SetCurPtr(e.BufLen, 0);
        e.ClipPaste();
        Pump(e);

        var buf = new char[e.BufLen];
        e.GetText(0, buf);
        Assert.Equal("one twoone", new string(buf));
    }
}
