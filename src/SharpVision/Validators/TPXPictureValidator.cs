using System.Globalization;
using System.Text;

using static SharpVision.Dialogs.MsgBox;
using static SharpVision.Validators.TPicResult;
using static SharpVision.Validators.ValidatorFlags;

namespace SharpVision.Validators;

/// <summary>
/// Validates input against a Paradox-style picture: <c>#</c> digit, <c>?</c> letter, <c>&amp;</c> letter
/// (uppercased), <c>!</c> any (uppercased), <c>@</c> any, <c>;</c> escapes the next character,
/// <c>*n</c> repeats, <c>[]</c> optional, <c>{}</c> group, <c>,</c> alternatives; other characters
/// are literals. With <c>voFill</c>, literals are typed automatically.
/// </summary>
/// Ported from source/tvision/tvalidat.cpp, source/tvision/tvtext2.cpp (errorMsg),
/// include/tvision/validate.h (class TPXPictureValidator).
public class TPXPictureValidator : TValidator
{
    /// <summary>The picture.</summary>
    protected string Pic;

    private int _index, _jndex;

    /// <summary>Creates a validator for <paramref name="aPic"/>; sets <c>vsSyntax</c> if the picture is malformed.</summary>
    public TPXPictureValidator(string aPic, bool autoFill)
    {
        Pic = aPic;
        if (autoFill)
            Options |= voFill;
        var s = "";
        if (Picture(ref s, false) != prEmpty)
            Status = vsSyntax;
    }

    /// <summary>Reports the expected picture in a message box.</summary>
    public override void Error() =>
        MessageBox(string.Create(CultureInfo.InvariantCulture, $"Error in picture format.\n {Pic}"), mfError | mfOKButton);

    /// <summary>Whether the partial input can still match; with <c>voFill</c> it may append literals.</summary>
    public override bool IsValidInput(ref string s, bool suppressFill)
    {
        var doFill = (Options & voFill) != 0 && !suppressFill;
        return Picture(ref s, doFill) != prError;
    }

    /// <summary>Whether the input fills the picture.</summary>
    public override bool IsValid(string s) => Picture(ref s, false) == prComplete;

    private static bool IsNumber(char ch) => '0' <= ch && ch <= '9';

    private static bool IsLetter(char ch)
    {
        ch = (char)(ch & 0xdf);
        return 'A' <= ch && ch <= 'Z';
    }

    private static bool IsSpecial(char ch, string special) => special.Contains(ch);

    private static bool IsComplete(TPicResult result) => result is prComplete or prAmbiguous;

    private static bool IsIncomplete(TPicResult result) => result is prIncomplete or prIncompNoFill;

    private static char Uppercase(char c) => 'a' <= c && c <= 'z' ? (char)(c + 'A' - 'a') : c;

    // The picture is a C string: reading at or past its end gives the terminator.
    private char P(int i) => i < Pic.Length ? Pic[i] : '\0';

    private void Consume(char ch, StringBuilder input)
    {
        input[_jndex] = ch;
        _index++;
        _jndex++;
    }

    // Skips a character or a picture group.
    private void ToGroupEnd(ref int i, int termCh)
    {
        int brkLevel = 0, brcLevel = 0;
        do
        {
            if (i == termCh)
                return;
            switch (P(i))
            {
                case '[': brkLevel++; break;
                case ']': brkLevel--; break;
                case '{': brcLevel++; break;
                case '}': brcLevel--; break;
                case ';': i++; break;
            }
            i++;
        } while (!(brkLevel == 0 && brcLevel == 0));
    }

    // Finds the next comma separator.
    private bool SkipToComma(int termCh)
    {
        do
        {
            ToGroupEnd(ref _index, termCh);
        } while (!(_index == termCh || P(_index) == ','));
        if (P(_index) == ',')
            _index++;
        return _index < termCh;
    }

    // Calculates the end of a group.
    private int CalcTerm(int termCh)
    {
        var k = _index;
        ToGroupEnd(ref k, termCh);
        return k;
    }

    // The next group is repeated n times, or any number of times when n is 0.
    private TPicResult Iteration(StringBuilder input, int inTerm)
    {
        var itr = 0;
        var rslt = prError;
        _index++; // Skip '*'.

        while (IsNumber(P(_index)))
        {
            itr = itr * 10 + (P(_index) - '0');
            _index++;
        }

        var k = _index;
        var termCh = CalcTerm(inTerm);

        if (itr != 0)
        {
            for (var l = 1; l <= itr; l++)
            {
                _index = k;
                rslt = Process(input, termCh);
                if (!IsComplete(rslt))
                {
                    // Empty means incomplete, since all are required.
                    if (rslt == prEmpty)
                        rslt = prIncomplete;
                    return rslt;
                }
            }
        }
        else
        {
            do
            {
                _index = k;
                rslt = Process(input, termCh);
            } while (rslt == prComplete);
            if (rslt is prEmpty or prError)
            {
                _index++;
                rslt = prAmbiguous;
            }
        }
        _index = termCh;
        return rslt;
    }

    // Processes a picture group.
    private TPicResult Group(StringBuilder input, int inTerm)
    {
        var termCh = CalcTerm(inTerm);
        _index++;
        var rslt = Process(input, termCh - 1);
        if (!IsIncomplete(rslt))
            _index = termCh;
        return rslt;
    }

    private TPicResult CheckComplete(TPicResult rslt, int termCh)
    {
        var j = _index;
        var status = true;
        if (IsIncomplete(rslt))
        {
            // Skip optional pieces.
            while (status)
            {
                switch (P(j))
                {
                    case '[':
                        ToGroupEnd(ref j, termCh);
                        break;
                    case '*':
                        if (!IsNumber(P(j + 1)))
                            j++;
                        ToGroupEnd(ref j, termCh);
                        break;
                    default:
                        status = false;
                        break;
                }
            }
            if (j == termCh)
                rslt = prAmbiguous;
        }
        return rslt;
    }

    private TPicResult Scan(StringBuilder input, int termCh)
    {
        var rScan = prError;
        var rslt = prEmpty;

        while (_index != termCh && P(_index) != ',')
        {
            if (_jndex >= input.Length)
                return CheckComplete(rslt, termCh);

            var ch = input[_jndex];
            switch (P(_index))
            {
                case '#':
                    if (!IsNumber(ch))
                        return prError;
                    Consume(ch, input);
                    break;
                case '?':
                    if (!IsLetter(ch))
                        return prError;
                    Consume(ch, input);
                    break;
                case '&':
                    if (!IsLetter(ch))
                        return prError;
                    Consume(Uppercase(ch), input);
                    break;
                case '!':
                    Consume(Uppercase(ch), input);
                    break;
                case '@':
                    Consume(ch, input);
                    break;
                case '*':
                    rslt = Iteration(input, termCh);
                    if (!IsComplete(rslt))
                        return rslt;
                    if (rslt == prError)
                        rslt = prAmbiguous;
                    break;
                case '{':
                    rslt = Group(input, termCh);
                    if (!IsComplete(rslt))
                        return rslt;
                    break;
                case '[':
                    rslt = Group(input, termCh);
                    if (IsIncomplete(rslt))
                        return rslt;
                    if (rslt == prError)
                        rslt = prAmbiguous;
                    break;
                default:
                    if (P(_index) == ';')
                        _index++;
                    // A space stands for the expected literal.
                    if (Uppercase(P(_index)) != Uppercase(ch) && ch != ' ')
                        return rScan;
                    Consume(P(_index), input);
                    break;
            }

            if (rslt == prAmbiguous)
                rslt = prIncompNoFill;
            else
                rslt = prIncomplete;
        }

        return rslt == prIncompNoFill ? prAmbiguous : prComplete;
    }

    private TPicResult Process(StringBuilder input, int termCh)
    {
        TPicResult rslt, rProcess;
        var incomp = false;
        int incompJ = 0, incompI = 0;
        var oldI = _index;
        var oldJ = _jndex;
        do
        {
            rslt = Scan(input, termCh);

            // Only accept completes if they make it farther in the input than the last incomplete.
            if (rslt == prComplete && incomp && _jndex < incompJ)
            {
                rslt = prIncomplete;
                _jndex = incompJ;
            }

            if (rslt is prError or prIncomplete)
            {
                rProcess = rslt;
                if (!incomp && rslt == prIncomplete)
                {
                    incomp = true;
                    incompI = _index;
                    incompJ = _jndex;
                }
                _index = oldI;
                _jndex = oldJ;
                if (!SkipToComma(termCh))
                {
                    if (incomp)
                    {
                        rProcess = prIncomplete;
                        _index = incompI;
                        _jndex = incompJ;
                    }
                    return rProcess;
                }
                oldI = _index;
            }
        } while (rslt is prError or prIncomplete);

        return rslt == prComplete && incomp ? prAmbiguous : rslt;
    }

    private bool SyntaxCheck()
    {
        if (Pic.Length == 0)
            return false;
        if (Pic[^1] == ';')
            return false;

        int i = 0, brkLevel = 0, brcLevel = 0;
        while (i < Pic.Length)
        {
            switch (Pic[i])
            {
                case '[': brkLevel++; break;
                case ']': brkLevel--; break;
                case '{': brcLevel++; break;
                case '}': brcLevel--; break;
                case ';': i++; break;
            }
            i++;
        }
        return brkLevel == 0 && brcLevel == 0;
    }

    /// <summary>
    /// Matches <paramref name="input"/> against the picture, normalizing case and literals in place;
    /// with <paramref name="autoFill"/>, appends the literals that follow incomplete input.
    /// </summary>
    public virtual TPicResult Picture(ref string input, bool autoFill)
    {
        if (!SyntaxCheck())
            return prSyntax;
        if (input.Length == 0)
            return prEmpty;

        var buf = new StringBuilder(input);
        _jndex = 0;
        _index = 0;
        var rslt = Process(buf, Pic.Length);
        if (rslt != prError && _jndex < buf.Length)
            rslt = prError;

        if (rslt == prIncomplete && autoFill)
        {
            var reprocess = false;
            while (_index < Pic.Length && !IsSpecial(Pic[_index], "#?&!@*{}[],"))
            {
                if (Pic[_index] == ';')
                    _index++;
                buf.Append(Pic[_index]);
                _index++;
                reprocess = true;
            }
            _jndex = 0;
            _index = 0;
            if (reprocess)
                rslt = Process(buf, Pic.Length);
        }
        input = buf.ToString();

        return rslt switch
        {
            prAmbiguous => prComplete,
            prIncompNoFill => prIncomplete,
            _ => rslt,
        };
    }
}
