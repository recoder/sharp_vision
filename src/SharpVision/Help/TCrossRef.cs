namespace SharpVision.Help;

/// <summary>A link from a span of a topic's text to another topic.</summary>
/// Ported from include/tvision/helpbase.h (class TCrossRef).
public struct TCrossRef
{
    /// <summary>The target topic's context number.</summary>
    public int Ref;

    /// <summary>One past the index of the link's first code unit in the topic's text (all paragraphs concatenated).</summary>
    public int Offset;

    /// <summary>The length of the link in UTF-16 code units.</summary>
    public byte Length;
}
