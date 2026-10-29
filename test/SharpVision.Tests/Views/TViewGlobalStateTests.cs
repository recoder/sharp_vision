using System;
using System.Collections.Generic;
using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Platform;
using SharpVision.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Tests.Views;

[Collection(GlobalStateCollection.Name)]
public sealed class TViewGlobalStateTests : IDisposable
{
    private readonly TCommandSet _savedCommands;

    public TViewGlobalStateTests()
    {
        TView.GetCommands(out _savedCommands);
    }

    public void Dispose()
    {
        TView.SetCommands(_savedCommands);
        TView.CommandSetChanged = false;
        THardwareInfo.Display = null;
    }

    [Fact]
    public void Should_StartWithWindowCommandsDisabled()
    {
        Assert.True(TView.CommandEnabled(cmQuit));
        Assert.False(TView.CommandEnabled(cmZoom));
        Assert.False(TView.CommandEnabled(cmClose));
        Assert.True(TView.CommandEnabled(1000));
    }

    [Fact]
    public void Should_FlagChange_When_CommandsToggled()
    {
        TView.CommandSetChanged = false;
        TView.EnableCommand(cmQuit);
        Assert.False(TView.CommandSetChanged);

        TView.DisableCommand(cmQuit);
        Assert.True(TView.CommandSetChanged);
        Assert.False(TView.CommandEnabled(cmQuit));
    }

    private sealed class Closer() : TView(new TRect(0, 0, 1, 1))
    {
        public override void HandleEvent(ref TEvent ev)
        {
            if (ev.What == evCommand)
            {
                EndModal(ev.Message.Command);
                ClearEvent(ref ev);
            }
        }
    }

    [Fact]
    public void Should_RunModalViewUntilEndModal_And_RestoreState()
    {
        var screen = new TestScreen(20, 10);
        var dialog = new TGroup(new TRect(0, 0, 10, 5));
        var closer = new Closer { Options = ofSelectable };
        dialog.Insert(closer);
        screen.Events.Enqueue(default);
        screen.Events.Enqueue(Ev.Command(cmOK));
        TView.DisableCommand(cmHelp);

        var result = screen.ExecView(dialog);

        Assert.Equal(cmOK, result);
        Assert.Null(dialog.Owner);
        Assert.False(dialog.GetState(sfModal));
        Assert.Null(dialog.TopView());
        Assert.False(TView.CommandEnabled(cmHelp));
    }

    private sealed class FakeDisplay : IDisplayOutput
    {
        public List<string> Calls { get; } = new();

        public void ScreenWrite(int x, int y, ReadOnlySpan<TScreenCell> cells) => Calls.Add($"write {x},{y} {cells.Length}");

        public void SetCaretPosition(int x, int y) => Calls.Add($"pos {x},{y}");

        public void SetCaretSize(int size) => Calls.Add($"size {size}");
    }

    [Fact]
    public void Should_PlaceCaretInScreenCoordinates_When_FocusedViewShowsCursor()
    {
        var display = new FakeDisplay();
        THardwareInfo.Display = display;
        var screen = new TestScreen(20, 10);
        screen.SetState(sfFocused, true);
        var group = new TGroup(new TRect(5, 2, 15, 8));
        var view = new TView(new TRect(1, 1, 5, 2)) { Options = ofSelectable };
        group.Insert(view);
        screen.Insert(group);
        view.SetCursor(2, 0);
        display.Calls.Clear();

        view.BlockCursor();
        view.ShowCursor();

        Assert.Equal("pos 8,3", display.Calls[^2]);
        Assert.Equal("size 100", display.Calls[^1]);
    }

    [Fact]
    public void Should_HideCaret_When_CursorCoveredBySibling()
    {
        var display = new FakeDisplay();
        THardwareInfo.Display = display;
        var screen = new TestScreen(20, 10);
        screen.SetState(sfFocused, true);
        var view = new TView(new TRect(0, 0, 5, 1)) { Options = ofSelectable };
        screen.Insert(view);
        screen.Insert(new TView(new TRect(0, 0, 2, 1)));
        view.ShowCursor();

        Assert.Equal("size 0", display.Calls[^1]);
    }

    [Fact]
    public void Should_SendWritesToDisplay_When_OwnerBufferIsScreenBuffer()
    {
        var display = new FakeDisplay();
        THardwareInfo.Display = display;
        var screen = new TestScreen(4, 1);
        TScreen.ScreenBuffer = screen.Buffer;
        try
        {
            screen.Insert(new FillView(new TRect(1, 0, 3, 1), 'x'));

            Assert.Contains("write 1,0 2", display.Calls);
        }
        finally
        {
            TScreen.ScreenBuffer = null;
        }
    }
}
