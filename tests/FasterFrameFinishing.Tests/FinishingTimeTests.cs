using FasterFrameFinishing.Core;
using Xunit;

namespace FasterFrameFinishing.Tests;

public class FinishingTimeTests
{
    private const float VanillaFinish = 2.0f; // verified StructureFrame.BuildStates[3].Tool.EntryTime
    private const float Tolerance = 1e-5f;

    private static FinishingSettings On(float multiplier = 5f, bool instant = false) => new(true, multiplier, instant, false);

    [Fact]
    public void Default_is_five_times_faster()
    {
        Assert.Equal(0.4f, FinishingTimeCalculator.Compute(VanillaFinish, On()), Tolerance);
    }

    [Theory]
    [InlineData(1f, 2.0f)]
    [InlineData(2f, 1.0f)]
    [InlineData(4f, 0.5f)]
    [InlineData(10f, 0.2f)]
    [InlineData(0.5f, 4.0f)] // slower than vanilla is allowed
    public void Multiplier_divides_vanilla_time(float multiplier, float expected)
    {
        Assert.Equal(expected, FinishingTimeCalculator.Compute(VanillaFinish, On(multiplier)), Tolerance);
    }

    [Fact]
    public void Multiplier_of_one_is_exactly_vanilla()
    {
        Assert.Equal(VanillaFinish, FinishingTimeCalculator.Compute(VanillaFinish, On(1f)));
    }

    [Fact]
    public void Very_high_multiplier_is_floored_at_minimum_duration()
    {
        Assert.Equal(FinishingTimeCalculator.MinimumEntryTime, FinishingTimeCalculator.Compute(VanillaFinish, On(100f)));
    }

    [Fact]
    public void Instant_uses_minimum_positive_duration_regardless_of_multiplier()
    {
        float t = FinishingTimeCalculator.Compute(VanillaFinish, On(1f, instant: true));
        Assert.Equal(FinishingTimeCalculator.MinimumEntryTime, t);
        Assert.True(t > 0f, "Duration must stay > 0 so the vanilla hold-to-construct coroutine still runs.");
    }

    [Fact]
    public void Disabled_returns_vanilla()
    {
        var off = new FinishingSettings(false, 5f, true, false);
        Assert.Equal(VanillaFinish, FinishingTimeCalculator.Compute(VanillaFinish, off));
        Assert.Equal(VanillaFinish, FinishingTimeCalculator.Compute(VanillaFinish, FinishingSettings.Vanilla));
        Assert.Equal(VanillaFinish, FinishingTimeCalculator.Compute(VanillaFinish, null));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void Unusable_vanilla_values_are_left_untouched(float vanilla)
    {
        Assert.Equal(vanilla, FinishingTimeCalculator.Compute(vanilla, On()));
    }

    [Fact]
    public void Never_lengthens_a_vanilla_value_already_below_the_floor()
    {
        Assert.Equal(0.01f, FinishingTimeCalculator.Compute(0.01f, On(5f)), Tolerance);
        Assert.Equal(0.01f, FinishingTimeCalculator.Compute(0.01f, On(instant: true)), Tolerance);
    }

    [Theory]
    [InlineData(5f, 5f)]
    [InlineData(1f, 1f)]
    [InlineData(0.01f, FinishingSettings.MinSpeedMultiplier)]
    [InlineData(1000f, FinishingSettings.MaxSpeedMultiplier)]
    [InlineData(0f, FinishingSettings.DefaultSpeedMultiplier)]
    [InlineData(-3f, FinishingSettings.DefaultSpeedMultiplier)]
    [InlineData(float.NaN, FinishingSettings.DefaultSpeedMultiplier)]
    [InlineData(float.PositiveInfinity, FinishingSettings.DefaultSpeedMultiplier)]
    [InlineData(float.NegativeInfinity, FinishingSettings.DefaultSpeedMultiplier)]
    public void Multiplier_is_validated(float input, float expected)
    {
        Assert.Equal(expected, new FinishingSettings(true, input, false, false).SpeedMultiplier);
    }
}
