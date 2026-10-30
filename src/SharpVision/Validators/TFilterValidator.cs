// Ported from source/tvision/tvalidat.cpp, source/tvision/tvtext2.cpp (errorMsg),
// include/tvision/validate.h (class TFilterValidator).

using static SharpVision.Dialogs.MsgBox;

namespace SharpVision.Validators;

/// <summary>Accepts only text made of the characters in <see cref="ValidChars"/>.</summary>
public class TFilterValidator : TValidator
{
    private const string ErrorMsg = "Invalid character in input";

    /// <summary>The allowed characters.</summary>
    protected string ValidChars;

    /// <summary>Creates a validator that allows only the characters in <paramref name="aValidChars"/>.</summary>
    public TFilterValidator(string aValidChars)
    {
        ValidChars = aValidChars;
    }

    /// <summary>Reports an invalid character in a message box.</summary>
    public override void Error() => MessageBox(ErrorMsg, mfError | mfOKButton);

    /// <inheritdoc />
    public override bool IsValidInput(ref string s, bool suppressFill) => AllValid(s);

    /// <inheritdoc />
    public override bool IsValid(string s) => AllValid(s);

    private bool AllValid(string s)
    {
        foreach (var c in s)
        {
            if (!ValidChars.Contains(c))
                return false;
        }
        return true;
    }
}
