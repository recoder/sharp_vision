namespace SharpVision.ColorSel;

/// <summary>The focused group and each group's focused item, kept between color dialogs.</summary>
/// Ported from include/tvision/colorsel.h (class TColorIndex).
public class TColorIndex
{
    /// <summary>The focused group.</summary>
    public byte GroupIndex;

    /// <summary>The number of groups.</summary>
    public byte ColorSize;

    /// <summary>The focused item of each group.</summary>
    public byte[] ColorIndex = new byte[256];
}
