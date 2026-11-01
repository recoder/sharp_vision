namespace SharpVision.Editors;

/// <summary>The data of the find dialog: the text and the <c>efXxx</c> options.</summary>
/// Ported from include/tvision/editors.h (struct TFindDialogRec).
public class TFindDialogRec(string find, ushort options)
{
    /// <summary>The text to find.</summary>
    public string Find = find;

    /// <summary>The <c>efXxx</c> options.</summary>
    public ushort Options = options;
}
