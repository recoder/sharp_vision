namespace SharpVision.Outline;

/// <summary>
/// Visits <paramref name="cur"/>, shown at display <paramref name="position"/> on outline
/// <paramref name="level"/>, with the continued levels in <paramref name="lines"/> and the
/// <c>ovXxx</c> <paramref name="flags"/>; returning true stops <see cref="TOutlineViewer.FirstThat"/>.
/// </summary>
/// Ported from include/tvision/outline.h (TOutlineVisitor, TOutlineVisitorNoArg).
/// <remarks>Closures replace upstream's <c>void*</c> argument.</remarks>
public delegate bool TOutlineVisitor(TOutlineViewer outLine, TNode cur, int level, int position, long lines, ushort flags);
