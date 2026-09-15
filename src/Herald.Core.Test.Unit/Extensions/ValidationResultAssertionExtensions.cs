using FluentValidation.Results;

namespace Herald.Core.Test.Unit.Extensions;

internal static class ValidationResultAssertionExtensions
{
    /// <summary>
    /// Asserts that validation failed exactly once, with a message that contains
    /// <paramref name="expectedSettingName"/> in quotes and <paramref name="expected"/>.
    /// </summary>
    public static void ShouldHaveSingleFailureFor(
        this ValidationResult subject,
        string expectedSettingName,
        string expected,
        string because = "",
        params object[] becauseArgs)
    {
        subject.Errors.Should().ContainSingle(because, becauseArgs)
            .Which.ErrorMessage.Should().Contain($"'{expectedSettingName}'", because, becauseArgs)
            .And.Contain(expected, because, becauseArgs);
    }
}
