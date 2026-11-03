namespace SharpVision.Outline;

/// <summary>The outline graph flags and the outline command.</summary>
/// Ported from include/tvision/outline.h (ovXxx, cmOutlineItemSelected).
public static class OutlineFlags
{
    /// <summary>The node is expanded, or has no children.</summary>
    public const ushort ovExpanded = 0x01;
    /// <summary>The node has children shown below it.</summary>
    public const ushort ovChildren = 0x02;
    /// <summary>The node is the last child of its parent.</summary>
    public const ushort ovLast = 0x04;

    /// <summary>Command an application can send when an outline item is selected.</summary>
    public const ushort cmOutlineItemSelected = 301;
}
