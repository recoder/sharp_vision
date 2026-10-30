namespace SharpVision.Events;

/// <summary>
/// Event codes (<c>TEvent.What</c>), event masks, and mouse button, wheel and event flags.
/// </summary>
/// Ported from include/tvision/system.h (event codes and masks).
public static class EventCodes
{
    // Event codes

    /// <summary>Mouse button pressed.</summary>
    public const ushort evMouseDown = 0x0001;

    /// <summary>Mouse button released.</summary>
    public const ushort evMouseUp = 0x0002;

    /// <summary>Mouse moved.</summary>
    public const ushort evMouseMove = 0x0004;

    /// <summary>Mouse button held down (auto-repeat).</summary>
    public const ushort evMouseAuto = 0x0008;

    /// <summary>Mouse wheel scrolled.</summary>
    public const ushort evMouseWheel = 0x0020;

    /// <summary>Key pressed.</summary>
    public const ushort evKeyDown = 0x0010;

    /// <summary>Command.</summary>
    public const ushort evCommand = 0x0100;

    /// <summary>Broadcast message.</summary>
    public const ushort evBroadcast = 0x0200;

    // Event masks

    /// <summary>No event, or an event that has been handled.</summary>
    public const ushort evNothing = 0x0000;

    /// <summary>Any mouse event.</summary>
    public const ushort evMouse = 0x002f;

    /// <summary>Any keyboard event.</summary>
    public const ushort evKeyboard = 0x0010;

    /// <summary>Any message event (command, broadcast or user-defined).</summary>
    public const ushort evMessage = 0xFF00;

    // Mouse button state masks

    /// <summary>Left mouse button.</summary>
    public const byte mbLeftButton = 0x01;

    /// <summary>Right mouse button.</summary>
    public const byte mbRightButton = 0x02;

    /// <summary>Middle mouse button.</summary>
    public const byte mbMiddleButton = 0x04;

    // Mouse wheel state masks

    /// <summary>Wheel scrolled up.</summary>
    public const byte mwUp = 0x01;

    /// <summary>Wheel scrolled down.</summary>
    public const byte mwDown = 0x02;

    /// <summary>Wheel scrolled left.</summary>
    public const byte mwLeft = 0x04;

    /// <summary>Wheel scrolled right.</summary>
    public const byte mwRight = 0x08;

    // Mouse event flags

    /// <summary>The mouse moved.</summary>
    public const ushort meMouseMoved = 0x01;

    /// <summary>The event is a double click.</summary>
    public const ushort meDoubleClick = 0x02;

    /// <summary>The event is a triple click.</summary>
    public const ushort meTripleClick = 0x04;
}
