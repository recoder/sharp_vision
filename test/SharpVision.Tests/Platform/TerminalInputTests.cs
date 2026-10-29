using System;
using System.Text;
using SharpVision.Events;
using SharpVision.Platform;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Tests.Platform;

public sealed class AnsiColorTests
{
    [Theory]
    [InlineData("xterm-256color", null, 256)]
    [InlineData("xterm", null, 16)]
    [InlineData("linux", null, 8)]
    [InlineData("dumb", null, 0)]
    [InlineData("foot-direct", null, 256 * 256 * 256)]
    public void Should_GuessColorCountFromTerm(string term, string? colorterm, int expected)
    {
        var saved = (Environment.GetEnvironmentVariable("TERM"), Environment.GetEnvironmentVariable("COLORTERM"));
        try
        {
            Environment.SetEnvironmentVariable("TERM", term);
            Environment.SetEnvironmentVariable("COLORTERM", colorterm);
            Assert.Equal(expected, TermCap.GuessColorCount());
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM", saved.Item1);
            Environment.SetEnvironmentVariable("COLORTERM", saved.Item2);
        }
    }
}
