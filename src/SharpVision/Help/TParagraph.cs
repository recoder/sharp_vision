namespace SharpVision.Help;

/// <summary>A paragraph of a help topic, in a linked list.</summary>
/// Ported from include/tvision/helpbase.h (class TParagraph).
public class TParagraph
{
    /// <summary>The next paragraph, or null.</summary>
    public TParagraph? Next;

    /// <summary>Whether the paragraph is wrapped to the viewer width; otherwise each line is shown as is.</summary>
    public bool Wrap;

    /// <summary>The text, with lines ending in <c>'\n'</c>.</summary>
    public string Text = "";

    /// <summary>The length of <see cref="Text"/> in UTF-16 code units (bytes upstream).</summary>
    public int Size => Text.Length;
}
