using SharpVision.Objects;
using Xunit;

namespace SharpVision.Tests.Objects;

public sealed class TPointTests
{
    [Fact]
    public void Should_AddAndSubtractComponentWise()
    {
        var p = new TPoint(3, 4);
        var q = new TPoint(1, 2);

        Assert.Equal(new TPoint(4, 6), p + q);
        Assert.Equal(new TPoint(2, 2), p - q);
    }

    [Fact]
    public void Should_MutateInPlace_When_UsingCompoundAssignment()
    {
        var p = new TPoint(1, 1);
        p += new TPoint(2, 3);

        Assert.Equal(new TPoint(3, 4), p);
    }
}
