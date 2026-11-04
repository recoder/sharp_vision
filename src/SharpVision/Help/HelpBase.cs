using SharpVision.Streams;

namespace SharpVision.Help;

/// <summary>Writes a cross-reference target in place of its number (used by the help compiler for forward references).</summary>
/// Ported from include/tvision/helpbase.h (TCrossRefHandler).
public delegate void TCrossRefHandler(PStream s, int value);

/// <summary>The help file header and the cross-reference hook.</summary>
/// Ported from source/tvision/helpbase.cpp, include/tvision/helpbase.h (magicHeader, crossRefHandler, notAssigned).
public static class HelpBase
{
    /// <summary>The first four bytes of a help file, "FBHF".</summary>
    public const int magicHeader = 0x46484246;

    /// <summary>How <see cref="THelpTopic"/> writes cross-reference targets; <see cref="NotAssigned"/> writes them as is.</summary>
    public static TCrossRefHandler CrossRefHandler = NotAssigned;

    /// <summary>The default <see cref="CrossRefHandler"/>; does nothing.</summary>
    public static void NotAssigned(PStream s, int value)
    {
    }
}
