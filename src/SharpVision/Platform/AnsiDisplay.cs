// Ported from include/tvision/internal/ansiwrit.h, source/platform/ansiwrit.cpp
// (TermCap, AnsiScreenWriter) and source/platform/ncurdisp.cpp (NcursesDisplay).

using System;
using System.Buffers;
using System.Text;

using SharpVision.Drawing;
using SharpVision.Objects;

using static SharpVision.Drawing.StyleFlags;

namespace SharpVision.Platform;

/// <summary>Terminal color support levels.</summary>
internal enum TermCapColors : byte
{
    NoColor,
    Indexed8,
    Indexed16,
    Indexed256,
    Direct,
}

/// <summary>What the terminal can display.</summary>
internal readonly record struct TermCap(TermCapColors Colors, ushort Quirks)
{
    // Terminal quirk flags.
    public const ushort qfBoldIsBright = 0x0001;
    public const ushort qfBlinkIsBright = 0x0002;
    public const ushort qfNoItalic = 0x0004;
    public const ushort qfNoUnderline = 0x0008;

    /// <summary>
    /// Detects the color support from <c>COLORTERM</c> and <c>TERM</c>.
    /// </summary>
    /// <remarks>
    /// UPSTREAM: upstream gets the color count from terminfo through ncurses. Without
    /// curses, it is guessed from <c>TERM</c>: "*-256color" means 256 colors, "*-direct"
    /// direct color, "linux" 8 colors, "dumb" none, and anything else 16 colors (as upstream
    /// assumes for terminals that call themselves "xterm").
    /// </remarks>
    public static TermCap Detect(int colorCount)
    {
        var colorterm = Environment.GetEnvironmentVariable("COLORTERM");
        if (colorterm is "truecolor" or "24bit" || colorCount >= 256 * 256 * 256)
            return new TermCap(TermCapColors.Direct, 0);
        if (colorCount >= 256)
            return new TermCap(TermCapColors.Indexed256, 0);
        if (colorCount >= 16)
            return new TermCap(TermCapColors.Indexed16, 0);
        if (colorCount >= 8)
        {
            var quirks = qfBoldIsBright;
            if (Environment.GetEnvironmentVariable("TERM") == "linux")
                quirks |= qfBlinkIsBright | qfNoItalic | qfNoUnderline;
            return new TermCap(TermCapColors.Indexed8, quirks);
        }
        return new TermCap(TermCapColors.NoColor, 0);
    }

    /// <summary>Guesses the color count from <c>TERM</c>.</summary>
    public static int GuessColorCount()
    {
        var term = Environment.GetEnvironmentVariable("TERM") ?? "";
        if (term.EndsWith("-direct", StringComparison.Ordinal))
            return 256 * 256 * 256;
        if (term.Contains("256color", StringComparison.Ordinal))
            return 256;
        if (term is "dumb" or "")
            return 0;
        if (term == "linux")
            return 8;
        return 16;
    }
}

/// <summary>
/// A display that prints cells to the terminal with ANSI escape sequences, buffering the
/// output until <see cref="Flush"/>.
/// </summary>
internal sealed class AnsiDisplay : DisplayAdapter
{
    private const string CSI = "\x1B[";

    private enum TermColorType : byte
    {
        Default,
        Indexed,
        RGB,
        NoColor,
    }

    // A color as printed: an index or an RGB value plus its type.
    private readonly record struct TermColor(TermColorType Type, uint Value);

    private readonly record struct TermAttr(TermColor Fg, TermColor Bg, ushort Style);

    private readonly ConsoleCtl _con;
    private readonly int _colorCount;
    private readonly TermCap _termcap;
    private readonly ArrayBufferWriter<byte> _buf = new(4096);
    private readonly StringBuilder _sb = new();
    private TPoint _caretPos = new(-1, -1);
    private TermAttr _lastAttr;

    /// <param name="con">The console to write to.</param>
    /// <param name="colorCount">The number of colors, or null to guess it from the environment.</param>
    public AnsiDisplay(ConsoleCtl con, int? colorCount = null)
    {
        _con = con;
        _colorCount = colorCount ?? TermCap.GuessColorCount();
        _termcap = TermCap.Detect(_colorCount);
    }

    public override TPoint ReloadScreenInfo()
    {
        var size = _con.GetSize();
        Reset();
        return size;
    }

    public override int GetColorCount() => _colorCount;

    /// <summary>Resets the attributes and forgets the caret position.</summary>
    public void Reset()
    {
        Push(CSI + "0m");
        _caretPos = new TPoint(-1, -1);
        _lastAttr = default;
    }

    public override void ClearScreen()
    {
        Push(CSI + "0m" + CSI + "2J");
        _lastAttr = default;
    }

    public override void WriteCell(TPoint pos, ReadOnlySpan<char> text, TColorAttr attr, bool doubleWidth)
    {
        _sb.Clear();
        // Move caret. When the movement is only horizontal, a shorter sequence suffices.
        if (pos.Y != _caretPos.Y)
            _sb.Append(CSI).Append(pos.Y + 1).Append(';').Append(pos.X + 1).Append('H');
        else if (pos.X != _caretPos.X)
            _sb.Append(CSI).Append(pos.X + 1).Append('G');

        ConvertAttributes(attr);
        _sb.Append(text);
        Push(_sb);

        _caretPos = new TPoint(pos.X + 1 + (doubleWidth ? 1 : 0), pos.Y);
    }

    public override void SetCaretPosition(TPoint pos)
    {
        Push($"{CSI}{pos.Y + 1};{pos.X + 1}H");
        _caretPos = pos;
    }

    public override void SetCaretSize(int size)
    {
        // UPSTREAM: upstream calls ncurses' curs_set (invisible, normal, very visible).
        // Here: hide, or show a block (100%) or underline cursor with DECSCUSR.
        if (size <= 0)
            Push(CSI + "?25l");
        else
            Push(size >= 100 ? CSI + "?25h" + CSI + "2 q" : CSI + "?25h" + CSI + "4 q");
    }

    public override void Flush()
    {
        _con.Write(_buf.WrittenSpan);
        _buf.Clear();
    }

    private void Push(string s)
    {
        _sb.Clear();
        _sb.Append(s);
        Push(_sb);
    }

    private void Push(StringBuilder sb)
    {
        foreach (var chunk in sb.GetChunks())
        {
            var span = _buf.GetSpan(Encoding.UTF8.GetMaxByteCount(chunk.Length));
            _buf.Advance(Encoding.UTF8.GetBytes(chunk.Span, span));
        }
    }

    // Attribute conversion

    private void ConvertAttributes(TColorAttr c)
    {
        var style = c.Style;
        var fg = ConvertColor(c.Foreground, ref style, true);
        var bg = ConvertColor(c.Background, ref style, false);

        if ((_termcap.Quirks & TermCap.qfNoItalic) != 0)
            style &= unchecked((ushort)~slItalic);
        if ((_termcap.Quirks & TermCap.qfNoUnderline) != 0)
            style &= unchecked((ushort)~slUnderline);

        var attr = new TermAttr(fg, bg, style);
        WriteAttributes(attr, _lastAttr);
        _lastAttr = attr;
    }

    private void WriteAttributes(TermAttr attr, TermAttr lastAttr)
    {
        _sb.Append(CSI);

        WriteFlag(attr, lastAttr, slBold, "1", "22");
        WriteFlag(attr, lastAttr, slItalic, "3", "23");
        WriteFlag(attr, lastAttr, slUnderline, "4", "24");
        WriteFlag(attr, lastAttr, slBlink, "5", "25");
        WriteFlag(attr, lastAttr, slReverse, "7", "27");
        WriteFlag(attr, lastAttr, slStrike, "9", "29");

        if (attr.Fg != lastAttr.Fg)
            WriteColor(attr.Fg, true);
        if (attr.Bg != lastAttr.Bg)
            WriteColor(attr.Bg, false);

        if (_sb[^1] == ';')
            _sb[^1] = 'm';
        else
            _sb.Length -= CSI.Length;
    }

    private void WriteFlag(TermAttr attr, TermAttr lastAttr, ushort mask, string on, string off)
    {
        if ((attr.Style & mask) != (lastAttr.Style & mask))
            _sb.Append((attr.Style & mask) != 0 ? on : off).Append(';');
    }

    // RGB and XTerm256 colors get a separate SGR sequence because some terminal
    // emulators may otherwise have trouble processing them.
    private void SplitSGR()
    {
        if (_sb[^1] == ';')
        {
            _sb[^1] = 'm';
            _sb.Append(CSI);
        }
    }

    private void WriteColor(TermColor color, bool isFg)
    {
        switch (color.Type)
        {
            case TermColorType.Default:
                _sb.Append(isFg ? "39;" : "49;");
                break;
            case TermColorType.Indexed:
                var idx = (int)color.Value;
                if (idx >= 16)
                {
                    SplitSGR();
                    _sb.Append(isFg ? "38;5;" : "48;5;").Append(idx).Append(';');
                    SplitSGR();
                }
                else if (idx >= 8)
                    _sb.Append(idx - 8 + (isFg ? 90 : 100)).Append(';');
                else
                    _sb.Append(idx + (isFg ? 30 : 40)).Append(';');
                break;
            case TermColorType.RGB:
                SplitSGR();
                var rgb = new TColorRGB(color.Value);
                _sb.Append(isFg ? "38;2;" : "48;2;")
                   .Append(rgb.Red).Append(';').Append(rgb.Green).Append(';').Append(rgb.Blue).Append(';');
                SplitSGR();
                break;
            case TermColorType.NoColor:
                break;
        }
    }

    private TermColor ConvertColor(TColor color, ref ushort style, bool isFg) => _termcap.Colors switch
    {
        TermCapColors.NoColor => ConvertNoColor(color, ref style, isFg),
        TermCapColors.Indexed8 => ConvertIndexed8(color, ref style, isFg),
        TermCapColors.Indexed16 => ConvertIndexed16(color),
        TermCapColors.Indexed256 => ConvertIndexed256(color),
        _ => ConvertDirect(color),
    };

    private static TermColor ConvertNoColor(TColor color, ref ushort style, bool isFg)
    {
        // Mimic the mono palettes with styles.
        if (color.IsBIOS)
        {
            var bios = (byte)color.AsBIOS();
            if (isFg)
            {
                if ((bios & 0x8) != 0)
                    style |= slBold;
                else if (bios == 0x1)
                    style |= slUnderline;
            }
            else if ((bios & 0x7) == 0x7)
                style |= slReverse;
        }
        return new TermColor(TermColorType.NoColor, 0);
    }

    private TermColor ConvertIndexed8(TColor color, ref ushort style, bool isFg)
    {
        var cnv = ConvertIndexed16(color);
        if (cnv.Type == TermColorType.Indexed && cnv.Value >= 8)
        {
            cnv = cnv with { Value = cnv.Value - 8 };
            if (isFg)
            {
                if ((_termcap.Quirks & TermCap.qfBoldIsBright) != 0)
                    style |= slBold;
            }
            else if ((_termcap.Quirks & TermCap.qfBlinkIsBright) != 0)
                style |= slBlink;
        }
        return cnv;
    }

    private static TermColor ConvertIndexed16(TColor color)
    {
        if (color.IsBIOS)
            return new TermColor(TermColorType.Indexed, (byte)TColorConversion.BIOStoXTerm16(color.AsBIOS()));
        if (color.IsXTerm)
        {
            var idx = color.AsXTerm();
            if (idx >= 16)
                idx = TColorConversion.XTerm256toXTerm16(idx);
            return new TermColor(TermColorType.Indexed, idx);
        }
        if (color.IsRGB)
            return new TermColor(TermColorType.Indexed, (byte)TColorConversion.RGBtoXTerm16(color.AsRGB()));
        return new TermColor(TermColorType.Default, 0);
    }

    private static TermColor ConvertIndexed256(TColor color)
    {
        if (color.IsXTerm)
            return new TermColor(TermColorType.Indexed, (byte)color.AsXTerm());
        if (color.IsRGB)
            return new TermColor(TermColorType.Indexed, (byte)TColorConversion.RGBtoXTerm256(color.AsRGB()));
        return ConvertIndexed16(color);
    }

    private static TermColor ConvertDirect(TColor color)
    {
        if (color.IsRGB)
            return new TermColor(TermColorType.RGB, (uint)color.AsRGB());
        return ConvertIndexed256(color);
    }
}
