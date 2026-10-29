// Ported from include/tvision/views.h (sfXxx, ofXxx, gfXxx, dmXxx, hcXxx, sbXxx,
// wfXxx, noXxx, wnXxx and wpXxx constants, and event masks).

using static SharpVision.Events.EventCodes;

namespace SharpVision.Views;

/// <summary>
/// View state, option, grow mode and drag mode flags, and the other constants that
/// <c>views.h</c> declares alongside the command codes.
/// </summary>
public static class ViewFlags
{
    // TView State masks

    /// <summary>View state flag.</summary>
    public const ushort sfVisible = 0x001;

    /// <summary>View state flag.</summary>
    public const ushort sfCursorVis = 0x002;

    /// <summary>View state flag.</summary>
    public const ushort sfCursorIns = 0x004;

    /// <summary>View state flag.</summary>
    public const ushort sfShadow = 0x008;

    /// <summary>View state flag.</summary>
    public const ushort sfActive = 0x010;

    /// <summary>View state flag.</summary>
    public const ushort sfSelected = 0x020;

    /// <summary>View state flag.</summary>
    public const ushort sfFocused = 0x040;

    /// <summary>View state flag.</summary>
    public const ushort sfDragging = 0x080;

    /// <summary>View state flag.</summary>
    public const ushort sfDisabled = 0x100;

    /// <summary>View state flag.</summary>
    public const ushort sfModal = 0x200;

    /// <summary>View state flag.</summary>
    public const ushort sfDefault = 0x400;

    /// <summary>View state flag.</summary>
    public const ushort sfExposed = 0x800;

    // TView Option masks

    /// <summary>View option flag.</summary>
    public const ushort ofSelectable = 0x001;

    /// <summary>View option flag.</summary>
    public const ushort ofTopSelect = 0x002;

    /// <summary>View option flag.</summary>
    public const ushort ofFirstClick = 0x004;

    /// <summary>View option flag.</summary>
    public const ushort ofFramed = 0x008;

    /// <summary>View option flag.</summary>
    public const ushort ofPreProcess = 0x010;

    /// <summary>View option flag.</summary>
    public const ushort ofPostProcess = 0x020;

    /// <summary>View option flag.</summary>
    public const ushort ofBuffered = 0x040;

    /// <summary>View option flag.</summary>
    public const ushort ofTileable = 0x080;

    /// <summary>View option flag.</summary>
    public const ushort ofCenterX = 0x100;

    /// <summary>View option flag.</summary>
    public const ushort ofCenterY = 0x200;

    /// <summary>View option flag.</summary>
    public const ushort ofCentered = 0x300;

    /// <summary>View option flag.</summary>
    public const ushort ofValidate = 0x400;

    // TView GrowMode masks

    /// <summary>View grow mode flag.</summary>
    public const ushort gfGrowLoX = 0x01;

    /// <summary>View grow mode flag.</summary>
    public const ushort gfGrowLoY = 0x02;

    /// <summary>View grow mode flag.</summary>
    public const ushort gfGrowHiX = 0x04;

    /// <summary>View grow mode flag.</summary>
    public const ushort gfGrowHiY = 0x08;

    /// <summary>View grow mode flag.</summary>
    public const ushort gfGrowAll = 0x0f;

    /// <summary>View grow mode flag.</summary>
    public const ushort gfGrowRel = 0x10;

    /// <summary>View grow mode flag.</summary>
    public const ushort gfFixed = 0x20;

    // TView DragMode masks

    /// <summary>View drag mode flag.</summary>
    public const ushort dmDragMove = 0x01;

    /// <summary>View drag mode flag.</summary>
    public const ushort dmDragGrow = 0x02;

    /// <summary>View drag mode flag.</summary>
    public const ushort dmDragGrowLeft = 0x04;

    /// <summary>View drag mode flag.</summary>
    public const ushort dmLimitLoX = 0x10;

    /// <summary>View drag mode flag.</summary>
    public const ushort dmLimitLoY = 0x20;

    /// <summary>View drag mode flag.</summary>
    public const ushort dmLimitHiX = 0x40;

    /// <summary>View drag mode flag.</summary>
    public const ushort dmLimitHiY = 0x80;

    /// <summary>View drag mode flag.</summary>
    public const ushort dmLimitAll = dmLimitLoX | dmLimitLoY | dmLimitHiX | dmLimitHiY;

    // TView Help context codes

    /// <summary>Help context.</summary>
    public const ushort hcNoContext = 0;

    /// <summary>Help context.</summary>
    public const ushort hcDragging = 1;

    // TScrollBar part codes

    /// <summary>Scroll bar part or option.</summary>
    public const ushort sbLeftArrow = 0;

    /// <summary>Scroll bar part or option.</summary>
    public const ushort sbRightArrow = 1;

    /// <summary>Scroll bar part or option.</summary>
    public const ushort sbPageLeft = 2;

    /// <summary>Scroll bar part or option.</summary>
    public const ushort sbPageRight = 3;

    /// <summary>Scroll bar part or option.</summary>
    public const ushort sbUpArrow = 4;

    /// <summary>Scroll bar part or option.</summary>
    public const ushort sbDownArrow = 5;

    /// <summary>Scroll bar part or option.</summary>
    public const ushort sbPageUp = 6;

    /// <summary>Scroll bar part or option.</summary>
    public const ushort sbPageDown = 7;

    /// <summary>Scroll bar part or option.</summary>
    public const ushort sbIndicator = 8;

    // TScrollBar options for TWindow.StandardScrollBar

    /// <summary>Scroll bar part or option.</summary>
    public const ushort sbHorizontal = 0x000;

    /// <summary>Scroll bar part or option.</summary>
    public const ushort sbVertical = 0x001;

    /// <summary>Scroll bar part or option.</summary>
    public const ushort sbHandleKeyboard = 0x002;

    // TWindow Flags masks

    /// <summary>Window flag.</summary>
    public const ushort wfMove = 0x01;

    /// <summary>Window flag.</summary>
    public const ushort wfGrow = 0x02;

    /// <summary>Window flag.</summary>
    public const ushort wfClose = 0x04;

    /// <summary>Window flag.</summary>
    public const ushort wfZoom = 0x08;

    // TView inhibit flags

    /// <summary>Inhibit flag.</summary>
    public const ushort noMenuBar = 0x0001;

    /// <summary>Inhibit flag.</summary>
    public const ushort noDeskTop = 0x0002;

    /// <summary>Inhibit flag.</summary>
    public const ushort noStatusLine = 0x0004;

    /// <summary>Inhibit flag.</summary>
    public const ushort noBackground = 0x0008;

    /// <summary>Inhibit flag.</summary>
    public const ushort noFrame = 0x0010;

    /// <summary>Inhibit flag.</summary>
    public const ushort noViewer = 0x0020;

    /// <summary>Inhibit flag.</summary>
    public const ushort noHistory = 0x0040;

    // TWindow number constants

    /// <summary>Window number.</summary>
    public const ushort wnNoNumber = 0;

    // TWindow palette entries

    /// <summary>Window palette.</summary>
    public const ushort wpBlueWindow = 0;

    /// <summary>Window palette.</summary>
    public const ushort wpCyanWindow = 1;

    /// <summary>Window palette.</summary>
    public const ushort wpGrayWindow = 2;

    // Event masks

    /// <summary>Events routed to the view under the mouse.</summary>
    public const ushort positionalEvents = evMouse & ~evMouseWheel;

    /// <summary>Events routed along the focus chain.</summary>
    public const ushort focusedEvents = evKeyboard | evCommand;
}
