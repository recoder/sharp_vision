// Ported from source/tvision/tvalidat.cpp, include/tvision/validate.h (class TLookupValidator).

namespace SharpVision.Validators;

/// <summary>Base class of validators that check complete input against a list with <see cref="Lookup"/>.</summary>
public class TLookupValidator : TValidator
{
    /// <summary>Whether <see cref="Lookup"/> finds <paramref name="s"/>.</summary>
    public override bool IsValid(string s) => Lookup(s);

    /// <summary>Whether <paramref name="s"/> is in the list. True by default.</summary>
    public virtual bool Lookup(string s) => true;
}
