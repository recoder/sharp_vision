using System;
using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Tests.Views;

public sealed class TGroupTests
{
    private static FillView Selectable(TRect r, char ch)
    {
        var v = new FillView(r, ch);
        v.Options |= ofSelectable;
        return v;
    }

    [Fact]
    public void Should_KeepCircularZOrder_When_InsertingAndRemoving()
    {
        var group = new TGroup(new TRect(0, 0, 10, 10));
        var a = new FillView(new TRect(0, 0, 1, 1), 'a');
        var b = new FillView(new TRect(0, 0, 1, 1), 'b');
        var c = new FillView(new TRect(0, 0, 1, 1), 'c');
        group.Insert(a);
        group.Insert(b);
        group.InsertBefore(c, a);

        Assert.Same(b, group.First());
        Assert.Same(a, group.Last);
        Assert.Equal([1, 2, 3], new[] { group.IndexOf(b), group.IndexOf(c), group.IndexOf(a) });
        Assert.Same(c, group.At(2));
        Assert.Same(b, c.PrevView());
        Assert.Null(a.NextView());

        group.Remove(c);
        Assert.Null(c.Owner);
        Assert.Same(a, b.Next);
        Assert.Same(b, a.Next);
        Assert.Equal(0, group.IndexOf(c));
    }

    [Fact]
    public void Should_SelectBackmostSelectableView_When_Inserting()
    {
        var group = new TestScreen(10, 10);
        group.SetState(sfFocused, true);
        var a = Selectable(new TRect(0, 0, 1, 1), 'a');
        var b = Selectable(new TRect(1, 0, 2, 1), 'b');

        group.Insert(a);
        group.Insert(b);

        Assert.Same(a, group.Current);
        Assert.True(a.GetState(sfSelected | sfFocused));
        Assert.False(b.GetState(sfSelected));
    }

    [Fact]
    public void Should_CycleSelection_When_SelectingNext()
    {
        var group = new TestScreen(10, 10);
        var a = Selectable(new TRect(0, 0, 1, 1), 'a');
        var b = Selectable(new TRect(1, 0, 2, 1), 'b');
        var c = Selectable(new TRect(2, 0, 3, 1), 'c');
        group.Insert(a);
        group.Insert(b);
        group.Insert(c);
        b.State |= sfDisabled;

        group.SelectNext(true);
        Assert.Same(c, group.Current);
        group.SelectNext(true);
        Assert.Same(a, group.Current);
        group.SelectNext(false);
        Assert.Same(c, group.Current);
    }

    private sealed class Invalid(TRect r) : FillView(r, 'i')
    {
        public override bool Valid(ushort command) => command != cmReleasedFocus;
    }

    [Fact]
    public void Should_KeepFocus_When_CurrentViewIsInvalid()
    {
        var group = new TestScreen(10, 10);
        var stubborn = new Invalid(new TRect(0, 0, 1, 1));
        stubborn.Options |= ofSelectable | ofValidate;
        var other = Selectable(new TRect(1, 0, 2, 1), 'o');
        group.Insert(stubborn);
        group.Insert(other);
        Assert.Same(stubborn, group.Current);

        Assert.False(other.Focus());
        Assert.Same(stubborn, group.Current);
    }

    [Fact]
    public void Should_RouteMouseEventToFrontmostViewUnderMouse()
    {
        var group = new TestScreen(10, 10);
        var back = new FillView(new TRect(0, 0, 5, 5), 'b');
        var front = new FillView(new TRect(2, 2, 4, 4), 'f');
        group.Insert(back);
        group.Insert(front);

        var ev = Ev.MouseDown(3, 3);
        group.HandleEvent(ref ev);
        ev = Ev.MouseDown(0, 0);
        group.HandleEvent(ref ev);

        Assert.Equal([evMouseDown], front.Handled);
        Assert.Equal([evMouseDown], back.Handled);
    }

    [Fact]
    public void Should_RouteFocusedEventsThroughPhases()
    {
        var group = new TestScreen(10, 10);
        var order = new System.Collections.Generic.List<string>();
        var pre = new Recorder("pre", order) { Options = ofPreProcess };
        var post = new Recorder("post", order) { Options = ofPostProcess };
        var current = new Recorder("current", order) { Options = ofSelectable };
        var bystander = new Recorder("bystander", order);
        group.Insert(post);
        group.Insert(current);
        group.Insert(pre);
        group.Insert(bystander);
        group.SetCurrent(current, TView.SelectMode.normalSelect);

        var ev = Ev.Command(cmHelp);
        group.HandleEvent(ref ev);

        Assert.Equal(["pre", "current", "post"], order);
    }

    private sealed class Recorder(string name, System.Collections.Generic.List<string> order) : TView(new TRect(0, 0, 1, 1))
    {
        public override void HandleEvent(ref TEvent ev)
        {
            if (ev.What == evCommand)
                order.Add(name);
        }
    }

    [Fact]
    public void Should_MapColorsThroughOwnerPalettes()
    {
        var root = new PaletteGroup([0x1F, 0x2E, 0x3D]);
        var child = new PaletteView([3, 9]);
        root.Insert(child);

        Assert.Equal(new TColorAttr(0x3D), child.MapColor(1));
        Assert.Equal(TView.ErrorAttr, child.MapColor(2)); // 9 is outside the owner's palette.
        Assert.Equal(TView.ErrorAttr, child.MapColor(0));
        Assert.Equal(TView.ErrorAttr, child.MapColor(3));
        var pair = child.GetColor(0x0101);
        Assert.Equal(new TColorAttr(0x3D), pair[0]);
        Assert.Equal(new TColorAttr(0x3D), pair[1]);
    }

    private sealed class PaletteGroup(byte[] palette) : TGroup(new TRect(0, 0, 10, 10))
    {
        private readonly TPalette _palette = new(palette);

        public override TPalette GetPalette() => _palette;
    }

    private sealed class PaletteView(byte[] palette) : TView(new TRect(0, 0, 1, 1))
    {
        private readonly TPalette _palette = new(palette);

        public override TPalette GetPalette() => _palette;
    }

    [Fact]
    public void Should_ConvertCoordinatesThroughOwners()
    {
        var root = new TGroup(new TRect(0, 0, 80, 25));
        var group = new TGroup(new TRect(10, 5, 50, 20));
        var view = new TView(new TRect(2, 3, 6, 4));
        root.Insert(group);
        group.Insert(view);

        Assert.Equal(new TPoint(13, 9), view.MakeGlobal(new TPoint(1, 1)));
        Assert.Equal(new TPoint(1, 1), view.MakeLocal(new TPoint(13, 9)));
        Assert.True(view.MouseInView(new TPoint(12, 8)));
        Assert.False(view.MouseInView(new TPoint(16, 8)));
    }

    [Fact]
    public void Should_GrowSubviews_When_GroupResized()
    {
        var group = new TGroup(new TRect(0, 0, 10, 10));
        var stretch = new TView(new TRect(1, 1, 9, 2)) { GrowMode = (byte)gfGrowHiX };
        var anchored = new TView(new TRect(8, 8, 9, 9)) { GrowMode = (byte)(gfGrowLoX | gfGrowHiX | gfGrowLoY | gfGrowHiY) };
        group.Insert(stretch);
        group.Insert(anchored);

        group.ChangeBounds(new TRect(0, 0, 15, 12));

        Assert.Equal(new TRect(1, 1, 14, 2), stretch.GetBounds());
        Assert.Equal(new TRect(13, 10, 14, 11), anchored.GetBounds());
    }

    [Fact]
    public void Should_RecoverOriginalSize_When_ShrunkAndRegrown()
    {
        var group = new TGroup(new TRect(0, 0, 10, 10));
        var view = new TView(new TRect(0, 0, 8, 1)) { GrowMode = (byte)gfGrowHiX };
        group.Insert(view);

        // Shrinking by 10 would make the width -2; it is clamped to 0 and the 2 columns
        // are remembered, so growing back by 10 restores the original 8, not 10.
        group.ChangeBounds(new TRect(0, 0, 0, 10));
        Assert.Equal(0, view.Size.X);
        group.ChangeBounds(new TRect(0, 0, 10, 10));

        Assert.Equal(8, view.Size.X);
    }

    [Fact]
    public void Should_FitSizeLimits_When_Locating()
    {
        var group = new TGroup(new TRect(0, 0, 10, 10));
        var view = new TView(new TRect(0, 0, 2, 2));
        group.Insert(view);

        view.GrowTo(30, -5);

        Assert.Equal(new TPoint(10, 0), view.Size);
        view.MoveTo(3, 4);
        Assert.Equal(new TPoint(3, 4), view.Origin);
    }

    [Fact]
    public void Should_ReturnHandler_When_MessageHandled()
    {
        var group = new TGroup(new TRect(0, 0, 10, 10));
        var handler = new CommandEater();
        group.Insert(handler);
        group.SetCurrent(handler, TView.SelectMode.normalSelect);

        Assert.Same(handler, TView.Message(group, evCommand, cmHelp, "info"));
        Assert.Null(TView.Message(group, evCommand, cmQuit, "info"));
        Assert.Null(TView.Message(null, evCommand, cmHelp, null));
    }

    private sealed class CommandEater() : TView(new TRect(0, 0, 1, 1))
    {
        public override void HandleEvent(ref TEvent ev)
        {
            if (ev.What == evCommand && ev.Message.Command == cmHelp)
                ClearEvent(ref ev);
        }
    }

    [Fact]
    public void Should_DistributeDataSlots()
    {
        var group = new TGroup(new TRect(0, 0, 10, 10));
        var first = new DataView();
        var second = new DataView();
        group.Insert(first);
        group.Insert(second);

        Assert.Equal(2, group.DataSize());
        group.SetData(new object?[] { "one", "two" });
        Assert.Equal("one", first.Value);
        Assert.Equal("two", second.Value);

        var rec = new object?[2];
        group.GetData(rec);
        Assert.Equal(["one", "two"], rec);
    }

    private sealed class DataView() : TView(new TRect(0, 0, 1, 1))
    {
        public object? Value { get; private set; }

        public override int DataSize() => 1;

        public override void GetData(Span<object?> rec) => rec[0] = Value;

        public override void SetData(ReadOnlySpan<object?> rec) => Value = rec[0];
    }

    [Fact]
    public void Should_RemoveAndDestroySubviews_When_ShutDown()
    {
        var root = new TestScreen(10, 10);
        var group = new TGroup(new TRect(0, 0, 5, 5));
        var child = new TView(new TRect(0, 0, 1, 1));
        group.Insert(child);
        root.Insert(group);

        TObject.Destroy(group);

        Assert.Null(group.Last);
        Assert.Null(child.Owner);
        Assert.Null(group.Owner);
        Assert.Null(root.Last);
    }
}
