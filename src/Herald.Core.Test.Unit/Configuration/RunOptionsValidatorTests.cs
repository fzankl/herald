using Herald.Core.Configuration;
using Herald.Core.Test.Unit.Extensions;

namespace Herald.Core.Test.Unit.Configuration;

public sealed class RunOptionsValidatorTests
{
    private readonly RunOptionsValidator _validator = new();

    [Theory]
    [InlineData(RunMode.Dry)]
    [InlineData(RunMode.Live)]
    public void Options___Supported_Mode___Passes(RunMode mode)
    {
        var result = _validator.Validate(new RunOptions { Mode = mode });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Options___Missing_Mode___Fails()
    {
        var result = _validator.Validate(new RunOptions { Mode = null });

        result.ShouldHaveSingleFailureFor(RunOptions.ModeSettingName, "is missing");
    }

    [Fact]
    public void Options___Unsupported_Mode___Fails()
    {
        var result = _validator.Validate(new RunOptions { Mode = (RunMode)1 });

        result.ShouldHaveSingleFailureFor(RunOptions.ModeSettingName, "unsupported value");
    }
}
