using SharpVision.Validators;
using Xunit;

using static SharpVision.Validators.ValidatorFlags;

namespace SharpVision.Tests.Validators;

public sealed class TRangeValidatorTests
{
    [Fact]
    public void Should_AcceptOnlyValidChars_When_Filtering()
    {
        var v = new TFilterValidator("abc");
        var s = "abca";

        Assert.True(v.IsValidInput(ref s, false));
        Assert.True(v.IsValid(""));
        Assert.False(v.IsValid("abd"));
    }

    [Fact]
    public void Should_AllowMinus_Only_When_MinIsNegative()
    {
        var s = "-5";

        Assert.False(new TRangeValidator(0, 10).IsValidInput(ref s, false));
        Assert.True(new TRangeValidator(-10, 10).IsValidInput(ref s, false));
        Assert.True(new TRangeValidator(-10, 10).IsValid("-5"));
    }

    [Fact]
    public void Should_CheckRange_Like_Sscanf()
    {
        var v = new TRangeValidator(0, 100);

        Assert.True(v.IsValid("42"));
        Assert.True(v.IsValid("+100"));
        Assert.False(v.IsValid("101"));
        Assert.False(v.IsValid(""));
        Assert.False(v.IsValid("+"));
        // sscanf stops at the second sign, so this reads 1.
        Assert.True(v.IsValid("1+2"));
    }

    [Fact]
    public void Should_TransferInt_Only_When_VoTransferSet()
    {
        var v = new TRangeValidator(0, 100);
        var s = "42";
        object? rec = null;

        Assert.Equal(0, v.Transfer(ref s, ref rec, TVTransfer.vtGetData));
        Assert.Null(rec);

        v.Options |= voTransfer;
        Assert.Equal(1, v.Transfer(ref s, ref rec, TVTransfer.vtGetData));
        Assert.Equal(42, rec);

        rec = 7;
        Assert.Equal(1, v.Transfer(ref s, ref rec, TVTransfer.vtSetData));
        Assert.Equal("7", s);
    }
}
