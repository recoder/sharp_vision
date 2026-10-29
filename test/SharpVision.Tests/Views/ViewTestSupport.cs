using System.Collections.Generic;
using System.Linq;
using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Tests.Views;

/// <summary>
/// A root group that stands in for the screen: it is exposed and buffered, so its subviews
/// draw into <see cref="TGroup.Buffer"/>. Events come from <see cref="Events"/>.
/// </summary>
internal sealed class TestScreen : TGroup
{
    public TestScreen(int width, int height)
        : base(new TRect(0, 0, width, height))
    {
        // Like a running application: selected (so clicks are not eaten by
        // TView.HandleEvent) and exposed with a buffer to draw into.
        State |= sfSelected;
        SetState(sfExposed, true);
        GetBuffer();
    }

    public Queue<TEvent> Events { get; } = new();

    public List<TEvent> PutBack { get; } = new();

    public string Row(int y) =>
        string.Concat(Buffer!.Skip(y * Size.X).Take(Size.X).Select(c =>
            c.Character.IsWideCharTrail ? "" : c.Character.IsSingleChar && c.Character.SingleChar == '\0' ? " " : c.Character.GetText()));

    public TColorAttr AttrAt(int x, int y) => Buffer![y * Size.X + x].Attribute;

    public override void GetEvent(ref TEvent ev) =>
        ev = Events.Count > 0 ? Events.Dequeue() : default;

    public override void PutEvent(ref TEvent ev) => PutBack.Add(ev);
}

/// <summary>A view that fills itself with one character and attribute and records the events it gets.</summary>
internal class FillView : TView
{
    private readonly char _ch;
    private readonly TColorAttr _attr;

    public FillView(TRect bounds, char ch, TColorAttr attr = default)
        : base(bounds)
    {
        _ch = ch;
        _attr = attr == default ? 0x07 : attr;
    }

    public List<ushort> Handled { get; } = new();

    public int DrawCount { get; private set; }

    public override void Draw()
    {
        DrawCount++;
        var b = new TDrawBuffer();
        b.MoveChar(0, _ch, _attr, Size.X);
        WriteLine(0, 0, Size.X, Size.Y, b);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What != evNothing)
            Handled.Add(ev.What == evCommand || ev.What == evBroadcast ? ev.Message.Command : ev.What);
    }
}

internal static class Ev
{
    public static TEvent Command(ushort command) =>
        new() { What = evCommand, Message = new MessageEvent { Command = command } };

    public static TEvent MouseDown(int x, int y) =>
        new() { What = evMouseDown, Mouse = new MouseEventType { Where = new TPoint(x, y), Buttons = mbLeftButton } };
}
