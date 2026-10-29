using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Tests.Views;

public sealed class TWindowTests
{
    // Maps every window palette entry straight to a BIOS attribute, so GetColor works
    // without an application palette.
    private sealed class PaletteScreen : TGroup
    {
        public PaletteScreen(TestScreen screen)
            : base(screen.GetExtent())
        {
            screen.Insert(this);
            // Like TDeskTop's background: upstream's FirstMatch (and so ResetCurrent after
            // ofTopSelect) starts at Last, so Last must not be selectable.
            Insert(new TView(GetExtent()));
        }

        public override TPalette GetPalette() => new(new byte[32]
        {
            0, 0, 0, 0, 0, 0, 0, 0x1F, 0x1F, 0x1A, 0x13, 0x13, 0x1E, 0x71, 0x00,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        });
    }

    private static (TestScreen, TGroup) Desk(int w = 30, int h = 10)
    {
        var screen = new TestScreen(w, h);
        return (screen, new PaletteScreen(screen));
    }

    [Fact]
    public void Should_DrawSingleFrameWithCenteredTitle_When_Inactive()
    {
        var (screen, desk) = Desk();
        var w = new TWindow(new TRect(0, 0, 20, 5), "Ab", 1);
        desk.Insert(w);
        w.SetState(sfSelected, false);
        w.DrawView();

        // Inactive: single lines, no icons, number at width - 7 (zoom enabled).
        Assert.Equal("┌─────── Ab ─1─────┐", screen.Row(0)[..20]);
        Assert.Equal("│", screen.Row(1)[..1]);
        Assert.Equal("└──────────────────┘", screen.Row(4)[..20]);
    }

    [Fact]
    public void Should_DrawDoubleFrameWithIcons_When_Active()
    {
        var (screen, desk) = Desk();
        var w = new TWindow(new TRect(0, 0, 20, 5), "Ab", 1);
        desk.Insert(w);

        Assert.True(w.GetState(sfActive));
        Assert.True(w.Frame!.GetState(sfActive));
        Assert.Equal("╔═[■]═══ Ab ═1═[↑]═╗", screen.Row(0)[..20]);
        Assert.Equal("║", screen.Row(1)[..1]);
        Assert.Equal("└─════════════════─┘", screen.Row(4)[..20]);
    }

    [Fact]
    public void Should_JoinFramedSubviewsIntoFrame()
    {
        var (screen, desk) = Desk();
        var w = new TWindow(new TRect(0, 0, 20, 6), null, wnNoNumber) { Flags = 0 };
        w.SetState(sfSelected, false);
        // Inserted after the frame, so it comes before it in the z-order traversal from Last.
        w.Insert(new TView(new TRect(3, 2, 8, 3)) { Options = ofFramed });
        desk.Insert(w);
        w.SetState(sfSelected, false);
        w.DrawView();

        Assert.Equal("┌──────────────────┐", screen.Row(0)[..20]);
        Assert.Equal("│ ┌─────┐", screen.Row(1)[..9]);
        Assert.Equal("│ └─────┘", screen.Row(3)[..9]);
    }

    [Fact]
    public void Should_EnableWindowCommands_When_Selected()
    {
        TView.DisableCommands(new TCommandSet() + cmClose + cmZoom + cmResize + cmNext + cmPrev);
        var (_, desk) = Desk();
        var w = new TWindow(new TRect(0, 0, 20, 5), "x", 1);
        desk.Insert(w);

        Assert.True(TView.CommandEnabled(cmClose));
        Assert.True(TView.CommandEnabled(cmZoom));

        w.SetState(sfSelected, false);
        Assert.False(TView.CommandEnabled(cmClose));
        Assert.False(w.GetState(sfActive));
    }

    [Fact]
    public void Should_ToggleZoom()
    {
        var (_, desk) = Desk(30, 10);
        var w = new TWindow(new TRect(2, 2, 20, 8), "x", 1);
        desk.Insert(w);

        var ev = Ev.Command(cmZoom);
        w.HandleEvent(ref ev);
        Assert.Equal(evNothing, ev.What);
        Assert.Equal(new TRect(0, 0, 30, 10), w.GetBounds());

        ev = Ev.Command(cmZoom);
        w.HandleEvent(ref ev);
        Assert.Equal(new TRect(2, 2, 20, 8), w.GetBounds());
    }

    [Fact]
    public void Should_RemoveItself_When_Closed()
    {
        var (_, desk) = Desk();
        var w = new TWindow(new TRect(0, 0, 20, 5), "x", 1);
        desk.Insert(w);

        var ev = Ev.Command(cmClose);
        w.HandleEvent(ref ev);

        Assert.Null(w.Owner);
        Assert.Null(w.Frame);
    }

    [Fact]
    public void Should_IgnoreClose_When_AddressedToAnotherWindow()
    {
        var (_, desk) = Desk();
        var w = new TWindow(new TRect(0, 0, 20, 5), "x", 1);
        desk.Insert(w);

        var ev = Ev.Command(cmClose);
        ev.Message.InfoPtr = new object();
        w.HandleEvent(ref ev);

        Assert.Same(desk, w.Owner);
        Assert.Equal(evCommand, ev.What);
    }

    [Fact]
    public void Should_PutCancel_When_ClosedWhileModal()
    {
        var (screen, desk) = Desk();
        var w = new TWindow(new TRect(0, 0, 20, 5), "x", 1);
        desk.Insert(w);
        w.State |= sfModal;

        var ev = Ev.Command(cmClose);
        w.HandleEvent(ref ev);

        Assert.Same(desk, w.Owner);
        Assert.Contains(screen.PutBack, e => e.What == evCommand && e.Message.Command == cmCancel);
    }

    [Fact]
    public void Should_PutCloseCommand_When_CloseIconClicked()
    {
        var (screen, desk) = Desk();
        var w = new TWindow(new TRect(0, 0, 20, 5), "x", 1);
        desk.Insert(w);
        // MouseEvent reads the release from the queue.
        screen.Events.Enqueue(new TEvent { What = evMouseUp, Mouse = new MouseEventType { Where = new TPoint(3, 0) } });

        var ev = Ev.MouseDown(3, 0);
        w.Frame!.HandleEvent(ref ev);

        Assert.Equal(evNothing, ev.What);
        var put = Assert.Single(screen.PutBack);
        Assert.Equal(cmClose, put.Message.Command);
        Assert.Same(w, put.Message.InfoPtr);
    }

    [Fact]
    public void Should_SelectWindow_When_NumberBroadcast()
    {
        var (_, desk) = Desk();
        var a = new TWindow(new TRect(0, 0, 20, 5), "a", 1);
        var b = new TWindow(new TRect(0, 0, 20, 5), "b", 2);
        desk.Insert(a);
        desk.Insert(b);
        Assert.Same(b, desk.Current);

        var ev = new TEvent { What = evBroadcast, Message = new MessageEvent { Command = cmSelectWindowNum, InfoInt = 1 } };
        a.HandleEvent(ref ev);

        Assert.Same(a, desk.Current);
    }

    [Fact]
    public void Should_MoveFocus_When_TabPressed()
    {
        var (_, desk) = Desk();
        var w = new TWindow(new TRect(0, 0, 20, 6), "x", 1);
        var v1 = new TView(new TRect(1, 1, 5, 2)) { Options = ofSelectable };
        var v2 = new TView(new TRect(1, 2, 5, 3)) { Options = ofSelectable };
        w.Insert(v1);
        w.Insert(v2);
        desk.Insert(w);
        Assert.Same(v2, w.Current);

        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = kbTab } };
        w.HandleEvent(ref ev);

        Assert.Equal(evNothing, ev.What);
        Assert.Same(v1, w.Current);
    }

    [Fact]
    public void Should_LimitSizeTo16x6()
    {
        var w = new TWindow(new TRect(0, 0, 20, 8), "x", 1);
        w.SizeLimits(out var min, out _);
        Assert.Equal(new TPoint(16, 6), min);
    }
}
