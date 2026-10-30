using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Tests.Dialogs;

public sealed class TLabelTests : CommandStateTest
{
    private static (Views.TestScreen Screen, TDialog Dialog, TLabel Label, TView A, TView B) Setup()
    {
        TView.ShowMarkers = false;
        var (screen, desk) = DialogDesk.Create();
        var d = new TDialog(new TRect(0, 0, 30, 8), "D");
        var a = new TView(new TRect(10, 2, 20, 3)) { Options = ViewFlags.ofSelectable };
        var b = new TView(new TRect(10, 4, 20, 5)) { Options = ViewFlags.ofSelectable };
        var label = new TLabel(new TRect(1, 2, 9, 3), "~N~ame", a);
        d.Insert(a);
        d.Insert(label);
        d.Insert(b);
        desk.Insert(d);
        return (screen, d, label, a, b);
    }

    [Fact]
    public void Should_DrawTextAfterOneSpace()
    {
        var (screen, _, _, _, _) = Setup();

        Assert.Equal(" Name   ", screen.Row(2)[1..9]);
    }

    [Fact]
    public void Should_FocusLink_When_HotKeyPressed()
    {
        var (_, d, label, a, b) = Setup();
        Assert.Same(b, d.Current);

        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = kbAltN } };
        label.HandleEvent(ref ev);

        Assert.Equal(evNothing, ev.What);
        Assert.Same(a, d.Current);
    }

    [Fact]
    public void Should_FocusLink_When_Clicked()
    {
        var (_, d, label, a, _) = Setup();

        var ev = Views.Ev.MouseDown(2, 2);
        label.HandleEvent(ref ev);

        Assert.Same(a, d.Current);
    }

    [Fact]
    public void Should_Highlight_When_LinkFocused()
    {
        var (screen, _, _, a, _) = Setup();
        // Column 3 is the "a" after the hotkey "N", which uses the shortcut color.
        var normal = (byte)screen.AttrAt(3, 2);

        // Focusing broadcasts cmReceivedFocus to the dialog's subviews.
        a.Focus();

        Assert.NotEqual(normal, (byte)screen.AttrAt(3, 2));
        // Selected text: TLabel entry 2 -> 0x08 -> TDialog entry 8 -> application 0x27.
        Assert.Equal(0x27, (byte)screen.AttrAt(3, 2));
    }

    [Fact]
    public void Should_ReplaceAndTruncateText_When_ParamTextSet()
    {
        var (screen, desk) = DialogDesk.Create();
        var t = new TParamText(new TRect(0, 0, 10, 1));
        desk.Insert(t);

        t.SetText($"{3} files");

        Assert.Equal("3 files   ", screen.Row(0)[..10]);
        t.SetText(new string('x', 300));
        Assert.Equal(255, t.GetTextLen());
    }
}
