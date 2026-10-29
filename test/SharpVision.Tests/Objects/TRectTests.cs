using SharpVision.Objects;
using Xunit;

namespace SharpVision.Tests.Objects;

public sealed class TRectTests
{
    [Fact]
    public void Should_TreatBottomRightAsExclusive_When_CheckingContains()
    {
        var r = new TRect(0, 0, 10, 5);

        Assert.True(r.Contains(new TPoint(0, 0)));
        Assert.True(r.Contains(new TPoint(9, 4)));
        Assert.False(r.Contains(new TPoint(10, 4)));
        Assert.False(r.Contains(new TPoint(9, 5)));
    }

    [Fact]
    public void Should_ShrinkOnEachSide_When_GrowingByNegativeAmount()
    {
        var r = new TRect(0, 0, 10, 10);
        r.Grow(-1, -1);

        Assert.Equal(new TRect(1, 1, 9, 9), r);
    }

    [Fact]
    public void Should_TranslateBothCorners_When_Moved()
    {
        var r = new TRect(1, 1, 3, 3);
        r.Move(2, -1);

        Assert.Equal(new TRect(3, 0, 5, 2), r);
    }

    [Fact]
    public void Should_BeEmpty_When_IntersectingDisjointRects()
    {
        var r = new TRect(0, 0, 5, 5);
        r.Intersect(new TRect(10, 10, 20, 20));

        Assert.True(r.IsEmpty());
    }

    [Fact]
    public void Should_ProduceOverlap_When_Intersecting()
    {
        var r = new TRect(0, 0, 10, 10);
        r.Intersect(new TRect(5, 2, 15, 8));

        Assert.Equal(new TRect(5, 2, 10, 8), r);
    }

    [Fact]
    public void Should_ProduceBoundingBox_When_Unioned()
    {
        var r = new TRect(0, 0, 2, 2);
        r.Union(new TRect(5, 5, 7, 9));

        Assert.Equal(new TRect(0, 0, 7, 9), r);
    }
}
