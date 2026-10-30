// Ported from source/tvision/tvalidat.cpp, source/tvision/tvtext2.cpp (errorMsg),
// include/tvision/validate.h (class TStringLookupValidator).

using System.Collections.Generic;

using static SharpVision.Dialogs.MsgBox;

namespace SharpVision.Validators;

/// <summary>Accepts only input that exactly matches one of <see cref="Strings"/>.</summary>
public class TStringLookupValidator : TLookupValidator
{
    private const string ErrorMsg = "Input is not in list of valid strings";

    /// <summary>The valid strings, or null.</summary>
    protected List<string>? Strings;

    /// <summary>Creates a validator that accepts the strings in <paramref name="aStrings"/>.</summary>
    public TStringLookupValidator(List<string>? aStrings)
    {
        Strings = aStrings;
    }

    /// <summary>Reports the invalid input in a message box.</summary>
    public override void Error() => MessageBox(ErrorMsg, mfError | mfOKButton);

    /// <summary>Whether <paramref name="s"/> is one of the strings (case-sensitive).</summary>
    public override bool Lookup(string s) => Strings?.Contains(s) == true;

    /// <summary>Replaces the list of valid strings.</summary>
    public void NewStringList(List<string>? aStrings) => Strings = aStrings;
}
