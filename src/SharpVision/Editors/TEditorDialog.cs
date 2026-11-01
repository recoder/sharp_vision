namespace SharpVision.Editors;

/// <summary>
/// Shows the editor dialog <paramref name="dialog"/> (<c>edXxx</c>) with its arguments and returns the
/// command that closed it.
/// </summary>
/// Ported from include/tvision/editors.h (TEditorDialog).
public delegate ushort TEditorDialog(int dialog, params object?[] args);
