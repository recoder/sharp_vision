using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;
using Xunit;

using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Tests.Dialogs;

public sealed class TDialogTests : CommandStateTest
{
    private static TEvent Key(ushort keyCode) =>
        new() { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = keyCode } };

    [Fact]
    public void Should_UseDialogDefaults()
    {
        var d = new TDialog(new TRect(0, 0, 30, 10), "T");

        Assert.Equal(0, d.GrowMode);
        Assert.Equal(wfMove | wfClose, d.Flags);
        Assert.Equal((short)dpGrayDialog, d.Palette);
        Assert.Equal(wnNoNumber, (ushort)d.Number);
        Assert.NotNull(d.Frame);
    }

    [Fact]
    public void Should_DrawGrayFrame()
    {
        var (screen, desk) = DialogDesk.Create();
        var d = new TDialog(new TRect(0, 0, 20, 5), "Hi");
        desk.Insert(d);

        // Active frame: close icon only, no zoom icon or number.
        Assert.Equal("╔═[■]═══ Hi ═══════╗", screen.Row(0)[..20]);
        // Active frame: TFrame entry 3 -> 0x02 -> TDialog entry 2 -> application 0x21.
        Assert.Equal(0x21, (byte)screen.AttrAt(0, 0));
    }

    [Fact]
    public void Should_PutCancel_When_EscPressed()
    {
        var (screen, desk) = DialogDesk.Create();
        var d = new TDialog(new TRect(0, 0, 20, 6), "x");
        desk.Insert(d);

        var ev = Key(kbEsc);
        d.HandleEvent(ref ev);

        Assert.Equal(evNothing, ev.What);
        var put = Assert.Single(screen.PutBack);
        Assert.Equal(evCommand, put.What);
        Assert.Equal(cmCancel, put.Message.Command);
    }

    [Fact]
    public void Should_BroadcastDefault_When_EnterPressed()
    {
        var (screen, desk) = DialogDesk.Create();
        var d = new TDialog(new TRect(0, 0, 20, 6), "x");
        desk.Insert(d);

        var ev = Key(kbEnter);
        d.HandleEvent(ref ev);

        var put = Assert.Single(screen.PutBack);
        Assert.Equal(evBroadcast, put.What);
        Assert.Equal(cmDefault, put.Message.Command);
    }

    [Theory]
    [InlineData(cmOK)]
    [InlineData(cmCancel)]
    [InlineData(cmYes)]
    [InlineData(cmNo)]
    public void Should_EndModal_When_ModalAndClosingCommand(ushort command)
    {
        var d = new TDialog(new TRect(0, 0, 20, 6), "x");
        d.State |= sfModal;

        var ev = new TEvent { What = evCommand, Message = new MessageEvent { Command = command } };
        d.HandleEvent(ref ev);

        Assert.Equal(evNothing, ev.What);
        Assert.Equal(command, d.EndState);
    }

    [Fact]
    public void Should_IgnoreClosingCommand_When_NotModal()
    {
        var d = new TDialog(new TRect(0, 0, 20, 6), "x");

        var ev = new TEvent { What = evCommand, Message = new MessageEvent { Command = cmOK } };
        d.HandleEvent(ref ev);

        Assert.Equal(evCommand, ev.What);
    }

    private sealed class InvalidView : TView
    {
        public InvalidView()
            : base(new TRect(1, 1, 2, 2))
        {
        }

        public override bool Valid(ushort command) => false;
    }

    [Fact]
    public void Should_BeValidForCancel_When_SubviewIsInvalid()
    {
        var d = new TDialog(new TRect(0, 0, 20, 6), "x");
        d.Insert(new InvalidView());

        Assert.True(d.Valid(cmCancel));
        Assert.False(d.Valid(cmOK));
    }
}
