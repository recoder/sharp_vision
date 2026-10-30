using System;

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Menus;
using SharpVision.Objects;
using SharpVision.Platform;
using SharpVision.Tests.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;

namespace SharpVision.Tests.Menus;

public sealed class TMenuPopupTests : CommandStateTest, IDisposable
{
    private readonly ushort _width = TScreen.ScreenWidth, _height = TScreen.ScreenHeight;
    private readonly TScreenCell[]? _buffer = TScreen.ScreenBuffer;
    private readonly ScriptedProgram _app;

    public TMenuPopupTests()
    {
        TScreen.ScreenWidth = 80;
        TScreen.ScreenHeight = 25;
        TScreen.ScreenBuffer = new TScreenCell[80 * 25];
        _app = new ScriptedProgram();
    }

    public new void Dispose()
    {
        TObject.Destroy(_app);
        TScreen.ScreenWidth = _width;
        TScreen.ScreenHeight = _height;
        TScreen.ScreenBuffer = _buffer;
        base.Dispose();
    }

    // 17 wide ("Exit" + 6 + "Alt-X" + 2) and 5 high.
    private static TMenuItem Items() =>
        new TMenuItem("~O~pen", cmOpen, kbF3, param: "F3") +
            TMenuItem.NewLine() +
            new TMenuItem("E~x~it", cmQuit, kbAltX, param: "Alt-X");

    private static TEvent Key(ushort keyCode, string? text = null) =>
        new() { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = keyCode, Text = text } };

    [Fact]
    public void Should_OpenBelowPoint_And_PutChosenCommandToReceiver()
    {
        var receiver = new TestScreen(1, 1);
        _app.Events.Enqueue(Key('x', "x"));

        var result = TMenuPopup.PopupMenu(new TPoint(10, 5), Items(), receiver);

        Assert.Equal(cmQuit, result);
        Assert.Equal(" ┌─────────────┐ ", _app.Snapshot[6].Substring(10, 17));
        // No default item is highlighted, so Open draws like Exit.
        Assert.Contains("Open", _app.Snapshot[7], StringComparison.Ordinal);
        var put = Assert.Single(receiver.PutBack);
        Assert.Equal((evCommand, cmQuit), (put.What, put.Message.Command));
    }

    [Fact]
    public void Should_OpenAbovePoint_When_NoRoomBelow()
    {
        _app.Events.Enqueue(Key(kbEsc));

        var result = TMenuPopup.PopupMenu(new TPoint(75, 23), Items());

        Assert.Equal(0, result);
        // Pushed left to fit the screen, and moved up so the box ends on the row of the point.
        Assert.Equal(" ┌─────────────┐ ", _app.Snapshot[18].Substring(63, 17));
        Assert.Equal(" └─────────────┘ ", _app.Snapshot[22].Substring(63, 17));
    }

    [Fact]
    public void Should_PutCommand_When_CtrlLetterPressed()
    {
        var screen = new TestScreen(30, 10);
        var popup = new TMenuPopup(new TRect(0, 0, 30, 10), new TMenu(Items()));
        screen.Insert(popup);

        var ev = Key(kbCtrlO);
        popup.HandleEvent(ref ev);
        Assert.Equal(evNothing, ev.What);
        Assert.Equal(cmOpen, Assert.Single(screen.PutBack).Message.Command);

        // An Alt key with no item is swallowed.
        ev = Key(kbAltQ);
        popup.HandleEvent(ref ev);
        Assert.Equal(evNothing, ev.What);
        Assert.Single(screen.PutBack);
    }
}
