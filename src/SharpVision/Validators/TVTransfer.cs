namespace SharpVision.Validators;

/// <summary>The operation requested from <see cref="TValidator.Transfer"/>.</summary>
/// Ported from include/tvision/validate.h (enum TVTransfer).
public enum TVTransfer
{
    /// <summary>Return the number of data slots the validator transfers.</summary>
    vtDataSize,

    /// <summary>Set the input line's text from the data slot.</summary>
    vtSetData,

    /// <summary>Write the input line's text into the data slot.</summary>
    vtGetData,
}
