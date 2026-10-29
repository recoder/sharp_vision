// Ported from include/tvision/system.h (struct TEvent).

namespace SharpVision.Events;

/// <summary>
/// An event: <see cref="What"/> says which kind (<c>evXxx</c>) and which of the other
/// fields holds its data.
/// </summary>
/// <remarks>
/// UPSTREAM: upstream overlaps <c>mouse</c>, <c>keyDown</c> and <c>message</c> in a union.
/// C# cannot overlap object references with other fields, so they are separate here. Only
/// the field matching <see cref="What"/> is meaningful.
/// </remarks>
public struct TEvent
{
    /// <summary>The event code (<c>evXxx</c>). <c>evNothing</c> when there is no event or it has been handled.</summary>
    public ushort What;

    /// <summary>Data of a mouse event.</summary>
    public MouseEventType Mouse;

    /// <summary>Data of a keyboard event.</summary>
    public KeyDownEvent KeyDown;

    /// <summary>Data of a message event.</summary>
    public MessageEvent Message;
}
