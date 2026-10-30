namespace SharpVision.Validators;

/// <summary>Base class of input line validators. Accepts any input.</summary>
/// Ported from source/tvision/tvalidat.cpp, include/tvision/validate.h (class TValidator).
public class TValidator
{
    /// <summary>The validator status (<c>vsXxx</c>).</summary>
    public ushort Status;

    /// <summary>The validator options (<c>voXxx</c>).</summary>
    public ushort Options;

    /// <summary>Reports invalid data to the user. Does nothing by default.</summary>
    public virtual void Error()
    {
    }

    /// <summary>Checks partial input while typing; may modify <paramref name="s"/> (e.g. auto-fill).</summary>
    public virtual bool IsValidInput(ref string s, bool suppressFill) => true;

    /// <summary>Checks complete input.</summary>
    public virtual bool IsValid(string s) => true;

    /// <summary>
    /// Transfers data between the input line's text <paramref name="s"/> and the data slot
    /// <paramref name="rec"/>. Returns the number of slots used, or 0 to let the input line
    /// transfer its text as is.
    /// </summary>
    public virtual ushort Transfer(ref string s, ref object? rec, TVTransfer flag) => 0;

    /// <summary>Checks <paramref name="s"/> with <see cref="IsValid"/>, calling <see cref="Error"/> when it fails.</summary>
    public bool Validate(string s)
    {
        if (!IsValid(s))
        {
            Error();
            return false;
        }
        return true;
    }
}
