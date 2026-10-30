using System.Globalization;

using static SharpVision.Dialogs.MsgBox;
using static SharpVision.Validators.ValidatorFlags;

namespace SharpVision.Validators;

/// <summary>
/// Accepts whole numbers between <see cref="Min"/> and <see cref="Max"/>. With <c>voTransfer</c>,
/// the input line's data slot holds the number as an <see cref="int"/> instead of the text.
/// </summary>
/// Ported from source/tvision/tvalidat.cpp, source/tvision/tvtext2.cpp (errorMsg, valid chars),
/// include/tvision/validate.h (class TRangeValidator).
/// <remarks>Creates a validator for the range <paramref name="aMin"/>..<paramref name="aMax"/>; '-' is allowed only if <paramref name="aMin"/> is negative.</remarks>
public class TRangeValidator(int aMin, int aMax) : TFilterValidator(aMin >= 0 ? ValidUnsignedChars : ValidSignedChars)
{
    private const string ValidUnsignedChars = "+0123456789";
    private const string ValidSignedChars = "+-0123456789";

    /// <summary>The smallest valid value.</summary>
    protected readonly int Min = aMin;

    /// <summary>The largest valid value.</summary>
    protected readonly int Max = aMax;

    /// <summary>Reports the valid range in a message box.</summary>
    public override void Error() =>
        MessageBox(string.Create(CultureInfo.InvariantCulture, $"Value not in the range {Min} to {Max}"), mfError | mfOKButton);

    /// <summary>Whether <paramref name="s"/> has only valid characters and starts with a number in range.</summary>
    public override bool IsValid(string s) =>
        base.IsValid(s) && ScanLong(s, out var value) && value >= Min && value <= Max;

    /// <summary>With <c>voTransfer</c>, converts between the text and an <see cref="int"/> slot.</summary>
    public override ushort Transfer(ref string s, ref object? rec, TVTransfer flag)
    {
        if ((Options & voTransfer) == 0)
            return 0;
        switch (flag)
        {
            case TVTransfer.vtGetData:
                // UPSTREAM: sscanf leaves value uninitialized when the text has no number; this gives 0.
                ScanLong(s, out var value);
                rec = (int)value;
                break;
            case TVTransfer.vtSetData:
                s = (rec is int v ? v : 0).ToString(CultureInfo.InvariantCulture);
                break;
        }
        return 1;
    }

    // Like sscanf("%ld"): skips leading white space, reads an optional sign and the digits that follow.
    private static bool ScanLong(string s, out long value)
    {
        value = 0;
        var i = 0;
        while (i < s.Length && char.IsWhiteSpace(s[i]))
            i++;
        var negative = false;
        if (i < s.Length && s[i] is '+' or '-')
            negative = s[i++] == '-';
        var start = i;
        while (i < s.Length && char.IsAsciiDigit(s[i]))
        {
            // shortcut: saturates like strtol instead of wrapping, past 18 digits.
            if (value < long.MaxValue / 10)
                value = value * 10 + (s[i] - '0');
            i++;
        }
        if (negative)
            value = -value;
        return i > start;
    }
}
