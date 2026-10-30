namespace SharpVision.Validators;

/// <summary>Validator status and option flags.</summary>
/// Ported from include/tvision/validate.h (vsXxx and voXxx constants).
public static class ValidatorFlags
{
    /// <summary>The validator is usable.</summary>
    public const ushort vsOk = 0;

    /// <summary>The validator's picture or setup has a syntax error.</summary>
    public const ushort vsSyntax = 1;

    /// <summary>Fill in literal characters automatically while typing.</summary>
    public const ushort voFill = 0x0001;

    /// <summary>The validator transfers the input line's data itself.</summary>
    public const ushort voTransfer = 0x0002;

    /// <summary>Reserved option bits.</summary>
    public const ushort voReserved = 0x00FC;
}
