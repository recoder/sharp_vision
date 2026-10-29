// Ported from include/tvision/ttext.h (struct TTextMetrics).

namespace SharpVision.Drawing;

/// <summary>Measurements of a piece of text, as returned by <see cref="TText.Measure"/>.</summary>
public readonly record struct TTextMetrics(int Width, int CharacterCount, int GraphemeCount);
