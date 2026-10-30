using SharpVision.Validators;
using Xunit;

using static SharpVision.Validators.TPicResult;
using static SharpVision.Validators.ValidatorFlags;

namespace SharpVision.Tests.Validators;

public sealed class TPXPictureValidatorTests
{
    private static (TPicResult Result, string Text) Pic(string pic, string input, bool autoFill = false)
    {
        var r = new TPXPictureValidator(pic, false).Picture(ref input, autoFill);
        return (r, input);
    }

    [Theory]
    [InlineData("(###) ###-####", "(555) 123-4567", prComplete)]
    [InlineData("(###) ###-####", "(55", prIncomplete)]
    [InlineData("(###) ###-####", "(5a", prError)]
    [InlineData("#[#]", "1", prComplete)]
    [InlineData("#[#]", "12", prComplete)]
    [InlineData("#[#]", "123", prError)]
    [InlineData("{Yes,No}", "No", prComplete)]
    [InlineData("{Yes,No}", "Maybe", prError)]
    [InlineData("*#", "12345", prComplete)]
    [InlineData("*3#", "12", prIncomplete)]
    [InlineData("*3#", "123", prComplete)]
    [InlineData("#", "", prEmpty)]
    public void Should_MatchPicture(string pic, string input, TPicResult expected)
    {
        Assert.Equal(expected, Pic(pic, input).Result);
    }

    [Theory]
    [InlineData("&&&", "abc", "ABC")]
    [InlineData("!@", "ab", "Ab")]
    [InlineData("{Yes,No}", "yes", "Yes")]
    [InlineData("##-##", "12 3", "12-3")]
    [InlineData("#;##", "1#", "1#")]
    public void Should_NormalizeInputInPlace(string pic, string input, string expected)
    {
        Assert.Equal(expected, Pic(pic, input).Text);
    }

    [Fact]
    public void Should_AppendLiterals_When_AutoFilling()
    {
        Assert.Equal((prIncomplete, "123-"), Pic("###-####", "123", autoFill: true));
        Assert.Equal((prIncomplete, "123"), Pic("###-####", "123"));
    }

    [Fact]
    public void Should_FillThroughIsValidInput_Only_When_VoFillAndNotSuppressed()
    {
        var v = new TPXPictureValidator("###-####", true);
        var s = "123";

        Assert.True(v.IsValidInput(ref s, true));
        Assert.Equal("123", s);
        Assert.True(v.IsValidInput(ref s, false));
        Assert.Equal("123-", s);
        Assert.False(v.IsValid(s));
        Assert.True(v.IsValid("123-4567"));
    }

    [Theory]
    [InlineData("[#")]
    [InlineData("#}")]
    [InlineData("#;")]
    [InlineData("")]
    public void Should_SetSyntaxStatus_When_PictureMalformed(string pic)
    {
        Assert.Equal(vsSyntax, new TPXPictureValidator(pic, false).Status);
    }
}
