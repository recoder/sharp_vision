using SharpVision.Platform;
using Xunit;

namespace SharpVision.Tests.Platform;

public sealed class CharOpsTests
{
    [Theory]
    [InlineData(0x0000, 0)]
    [InlineData(0x0001, -1)]
    [InlineData(0x007F, -1)]
    [InlineData(0x0080, -1)]
    [InlineData(0x0061, 1)]  // a
    [InlineData(0x00AD, 1)]  // Soft hyphen.
    [InlineData(0x00FE, 1)]  // þ
    [InlineData(0x0300, 0)]  // Combining grave accent.
    [InlineData(0x094D, 0)]  // Devanagari virama.
    [InlineData(0x200D, 0)]  // Zero width joiner.
    [InlineData(0x2500, 1)]  // ─
    [InlineData(0x4E2D, 2)]  // 中
    [InlineData(0xFF21, 2)]  // Fullwidth A.
    [InlineData(0x1F60A, 2)] // 😊
    [InlineData(0xD800, -1)] // Surrogate code point.
    public void Should_ReturnColumns_When_MeasuringCodePoint(int codePoint, int expected)
    {
        Assert.Equal(expected, CharOps.DefaultWidth(codePoint));
    }
}
