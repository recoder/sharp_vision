namespace SharpVision.Validators;

/// <summary>The result of matching input against a <see cref="TPXPictureValidator"/> picture.</summary>
/// Ported from include/tvision/validate.h (enum TPicResult).
public enum TPicResult
{
    /// <summary>The input fills the picture.</summary>
    prComplete,

    /// <summary>The input matches so far but is not complete.</summary>
    prIncomplete,

    /// <summary>The input is empty.</summary>
    prEmpty,

    /// <summary>The input does not match.</summary>
    prError,

    /// <summary>The picture itself is malformed.</summary>
    prSyntax,

    /// <summary>The input is complete but more could follow.</summary>
    prAmbiguous,

    /// <summary>Incomplete, and not to be auto-filled.</summary>
    prIncompNoFill,
}
