namespace SharpVision.Editors;

/// <summary>The data of the replace dialog: the text, its replacement and the <c>efXxx</c> options.</summary>
/// Ported from include/tvision/editors.h (struct TReplaceDialogRec).
public class TReplaceDialogRec(string find, string replace, ushort options)
{
    /// <summary>The text to find.</summary>
    public string Find = find;

    /// <summary>The replacement.</summary>
    public string Replace = replace;

    /// <summary>The <c>efXxx</c> options.</summary>
    public ushort Options = options;
}
