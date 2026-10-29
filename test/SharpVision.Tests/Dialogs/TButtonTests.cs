using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Tests.Views;
using SharpVision.Views;
using Xunit;

using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Tests.Dialogs;

public sealed class TButtonTests : CommandStateTest
{
    private const ushort cmTest = 200;

    // Controls are inserted before the dialog goes on the desk, as in real code: InsertBefore
    // does not activate a view inserted into an already active group.
    private static (TestScreen Screen, DialogDesk Desk, TDialog Dialog) Dialog(params TView[] views)
    {
        TView.ShowMarkers = false;
        var (screen, desk) = DialogDesk.Create();
        var d = new TDialog(new TRect(0, 0, 30, 10), "D");
        foreach (var v in views)
            d.Insert(v);
        desk.Insert(d);
        return (screen, desk, d);
    }

    private static ProbeButton Button(int y, string title, ushort flags = bfNormal)
    {
        TView.EnableCommand(cmTest);
        return new ProbeButton(new TRect(2, y, 12, y + 2), title, cmTest, flags);
    }

    private sealed class ProbeButton : TButton
    {
        public ProbeButton(TRect bounds, string title, ushort command, ushort flags)
            : base(bounds, title, command, flags)
        {
        }

        public bool IsDefault => AmDefault;
    }

    private static bool Default(ProbeButton b) => b.IsDefault;

    [Fact]
    public void Should_DrawTitleCenteredWithShadow()
    {
        var (screen, _, _) = Dialog(Button(2, "~O~k"));

        // s = 9; title "Ok" at i + l = 1 + (9 - 2 - 1) / 2 = 4, after the shadow-colored column 0.
        Assert.Equal("    Ok   ▄", screen.Row(2)[2..12]);
        Assert.Equal("  ▀▀▀▀▀▀▀▀", screen.Row(3)[2..12]);
    }

    [Fact]
    public void Should_UseSelectedColors_When_Focused()
    {
        var (screen, _, _) = Dialog(Button(2, "Ok"));

        // Selected text: TButton entry 3 -> 0x0C -> TDialog entry 12 -> application 0x2B.
        Assert.Equal(0x2B, (byte)screen.AttrAt(4, 2));
        // Shadow: TButton entry 8 -> 0x0F -> TDialog entry 15 -> application 0x2E.
        Assert.Equal(0x2E, (byte)screen.AttrAt(2, 3));
    }

    [Fact]
    public void Should_PutCommand_When_Clicked()
    {
        var b = Button(2, "Ok");
        var (screen, _, _) = Dialog(b);
        screen.Events.Enqueue(new TEvent { What = evMouseUp, Mouse = new MouseEventType { Where = new TPoint(5, 2) } });

        var ev = Ev.MouseDown(5, 2);
        b.HandleEvent(ref ev);

        Assert.Equal(evNothing, ev.What);
        var put = Assert.Single(screen.PutBack);
        Assert.Equal(evCommand, put.What);
        Assert.Equal(cmTest, put.Message.Command);
        Assert.Same(b, put.Message.InfoPtr);
    }

    [Fact]
    public void Should_IgnoreClick_When_OnShadow()
    {
        var b = Button(2, "Ok");
        var (screen, _, _) = Dialog(b);

        // Bottom row is the shadow.
        var ev = Ev.MouseDown(5, 3);
        b.HandleEvent(ref ev);

        Assert.Equal(evNothing, ev.What);
        Assert.Empty(screen.PutBack);
    }

    [Fact]
    public void Should_BeDisabled_When_CommandDisabled()
    {
        const ushort cmOff = 201;
        TView.DisableCommand(cmOff);
        var b = new TButton(new TRect(0, 0, 10, 2), "Ok", cmOff, bfNormal);

        Assert.True(b.GetState(sfDisabled));
    }

    [Fact]
    public void Should_PressAfterAnimation_When_HotKeyPressed()
    {
        var b = Button(2, "~O~k");
        var (screen, desk, _) = Dialog(b);

        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = kbAltO } };
        b.HandleEvent(ref ev);

        Assert.Equal(evNothing, ev.What);
        Assert.NotNull(desk.LastTimer);
        Assert.Empty(screen.PutBack);
        // Pressed: shifted right by one, no shadow on the right.
        Assert.Equal("     Ok   ", screen.Row(2)[2..12]);

        ev = new TEvent { What = evBroadcast, Message = new MessageEvent { Command = cmTimerExpired, InfoPtr = desk.LastTimer } };
        b.HandleEvent(ref ev);

        Assert.Equal(evNothing, ev.What);
        Assert.Equal(cmTest, Assert.Single(screen.PutBack).Message.Command);
    }

    [Fact]
    public void Should_Animate_When_DefaultBroadcast()
    {
        var b = Button(2, "Ok", bfDefault);
        var (_, desk, _) = Dialog(b);

        var ev = new TEvent { What = evBroadcast, Message = new MessageEvent { Command = cmDefault } };
        b.HandleEvent(ref ev);

        Assert.Equal(evNothing, ev.What);
        Assert.NotNull(desk.LastTimer);
    }

    [Fact]
    public void Should_TakeDefault_When_OtherButtonFocused()
    {
        var def = Button(2, "Ok", bfDefault);
        var other = Button(4, "No");
        var (_, _, d) = Dialog(def, other);

        // Inserting focuses `other`, which grabs the default from `def`.
        Assert.Same(other, d.Current);
        Assert.False(Default(def));
        Assert.True(Default(other));

        def.Select();
        Assert.True(Default(def));
        Assert.False(Default(other));
    }


    [Fact]
    public void Should_BroadcastCommand_When_BroadcastFlag()
    {
        var b = Button(2, "Ok", bfBroadcast);
        var listener = new FillView(new TRect(1, 8, 2, 9), ' ') { EventMask = evBroadcast };
        var (screen, _, _) = Dialog(listener, b);

        b.Press();

        Assert.Empty(screen.PutBack);
        Assert.Contains(cmRecordHistory, listener.Handled);
        Assert.Contains(cmTest, listener.Handled);
    }
}
