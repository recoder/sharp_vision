using SharpVision.Validators;
using Xunit;

namespace SharpVision.Tests.Validators;

public sealed class TStringLookupValidatorTests
{
    [Fact]
    public void Should_AcceptAnything_When_BaseLookup()
    {
        Assert.True(new TLookupValidator().IsValid("whatever"));
    }

    [Fact]
    public void Should_AcceptOnlyExactMatches_And_FollowNewList()
    {
        var v = new TStringLookupValidator(["Red", "Green"]);
        var partial = "Gr";

        Assert.True(v.IsValid("Green"));
        Assert.False(v.IsValid("green"));
        // Typing is never blocked; only complete input is checked.
        Assert.True(v.IsValidInput(ref partial, false));

        v.NewStringList(["Blue"]);
        Assert.False(v.IsValid("Red"));
        Assert.True(v.IsValid("Blue"));

        v.NewStringList(null);
        Assert.False(v.IsValid("Blue"));
    }
}
