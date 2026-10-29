using SharpVision.Events;
using Xunit;

namespace SharpVision.Tests.Events;

public sealed class TCommandSetTests
{
    [Fact]
    public void Should_BeEmpty_When_DefaultConstructed()
    {
        var set = new TCommandSet();

        Assert.True(set.IsEmpty());
        Assert.False(set.Has(0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(100)]
    [InlineData(255)]
    public void Should_ContainOnlyEnabledCommand(int cmd)
    {
        var set = new TCommandSet();
        set.EnableCmd(cmd);

        Assert.True(set.Has(cmd));
        Assert.False(set.Has(cmd ^ 1));
        set.DisableCmd(cmd);
        Assert.True(set.IsEmpty());
    }

    [Theory]
    [InlineData(256)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void Should_IgnoreCommand_When_OutOfRange(int cmd)
    {
        var set = new TCommandSet();
        set += cmd;

        Assert.True(set.IsEmpty());
        Assert.False(set.Has(cmd));
    }

    [Fact]
    public void Should_CopyOnAssignment()
    {
        var a = new TCommandSet();
        a += 5;
        var b = a;
        b += 6;

        Assert.False(a.Has(6));
        Assert.True(b.Has(5));
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Should_CombineSets()
    {
        var a = new TCommandSet() + 1 + 2;
        var b = new TCommandSet() + 2 + 3;

        Assert.Equal(new TCommandSet() + 2, a & b);
        Assert.Equal(new TCommandSet() + 1 + 2 + 3, a | b);
        Assert.Equal(new TCommandSet() + 1, a - b);

        a.EnableCmd(b);
        Assert.Equal(a | b, a);
        a.DisableCmd(b);
        Assert.Equal(new TCommandSet() + 1, a);
    }
}
