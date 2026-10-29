// Ported from include/tvision/system.h (struct MouseEventType).

using SharpVision.Objects;

namespace SharpVision.Events;

/// <summary>The data of a mouse event.</summary>
public struct MouseEventType
{
    /// <summary>Mouse position, in screen coordinates (made local by <c>TView.MakeLocal</c>).</summary>
    public TPoint Where;

    /// <summary>Mouse event flags (<c>meXxx</c>). Replaces Turbo Vision 1's <c>doubleClick</c>.</summary>
    public ushort EventFlags;

    /// <summary>Keyboard state (<c>kbShift</c>, <c>kbCtrlShift</c>, ...).</summary>
    public ushort ControlKeyState;

    /// <summary>Buttons held down (<c>mbXxx</c>).</summary>
    public byte Buttons;

    /// <summary>Wheel direction (<c>mwXxx</c>).</summary>
    public byte Wheel;
}
