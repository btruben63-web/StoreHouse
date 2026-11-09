using Product.Infrastructure.Persistence;

namespace Product.Api.UnitTest.Infrastructure;

public sealed class LikePatternEscaperTests
{
    [Theory]
    [InlineData("martelo", "martelo")]
    [InlineData("50%", @"50\%")]
    [InlineData("a_b", @"a\_b")]
    [InlineData(@"c:\temp", @"c:\\temp")]
    [InlineData(@"%_\", @"\%\_\\")]
    public void Escape_EscapesLikeWildcards(string input, string expected) =>
        Assert.Equal(expected, LikePatternEscaper.Escape(input));
}
