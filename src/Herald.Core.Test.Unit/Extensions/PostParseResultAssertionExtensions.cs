using Herald.Core.Models;
using Herald.Core.Parsing;

namespace Herald.Core.Test.Unit.Extensions;

internal static class PostParseResultAssertionExtensions
{
    /// <summary>
    /// Asserts that the file was rejected with exactly one message, and that the message names the
    /// subject and contains <paramref name="expected"/>.
    /// </summary>
    public static void ShouldBeRejectedWith(
        this PostParseResult subject,
        string expectedSubject,
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        subject.Post.Should()
            .BeNull(because, becauseArgs);

        subject.Errors.Should()
            .ContainSingle(because, becauseArgs)
            .Which
            .Should()
            .Contain(expectedSubject, because, becauseArgs)
            .And
            .Contain(expected, because, becauseArgs);
    }
}
