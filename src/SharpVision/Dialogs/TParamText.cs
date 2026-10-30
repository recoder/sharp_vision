// Ported from source/tvision/tparamte.cpp, include/tvision/dialogs.h (class TParamText).

using SharpVision.Objects;

namespace SharpVision.Dialogs;

/// <summary>Static text whose contents are replaced at run time with <see cref="SetText"/>.</summary>
/// <remarks>Palette layout: 1 = text.</remarks>
public class TParamText : TStaticText
{
    private const int MaxLength = 255;

    /// <summary>The current text.</summary>
    protected string Str = "";

    /// <summary>Creates an empty parameterized text view.</summary>
    public TParamText(TRect bounds)
        : base(bounds, null)
    {
    }

    /// <inheritdoc />
    public override string GetText() => Str;

    /// <summary>Replaces the text (at most 255 code units) and redraws.</summary>
    /// <remarks>Upstream takes a <c>printf</c> format and arguments; format the string before passing it.</remarks>
    public virtual void SetText(string text)
    {
        Str = text.Length > MaxLength ? text[..MaxLength] : text;
        DrawView();
    }

    /// <summary>The length of the text.</summary>
    public virtual int GetTextLen() => Str.Length;
}
