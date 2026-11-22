#if HAS_BEPINEX
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using FasterFrameFinishing.Configuration;
using FasterFrameFinishing.Core;
using Xunit;

namespace FasterFrameFinishing.Tests;

/// <summary>
/// Binds the real FinishingConfig against BepInEx's real ConfigFile (from the local install) and
/// checks defaults, the generated .cfg text, the speed drop-down options and validation of hand edits.
/// Compiled only when BepInEx.dll is available (see the test .csproj).
/// </summary>
public sealed class ConfigBindingTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"fff-test-{Guid.NewGuid():N}.cfg");

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    private FinishingConfig Bind() => FinishingConfig.Bind(new ConfigFile(_path, saveOnInit: true));

    private void WriteFile(string speed) =>
        File.WriteAllText(_path, $"[Frame Finishing]\nEnabled = true\nSpeedMultiplier = {speed}\nInstantFinish = false\nDebugLogging = false\n");

    [Fact]
    public void Defaults_match_specification()
    {
        FinishingConfig config = Bind();
        FinishingSettings s = config.CreateSettings();

        Assert.True(s.Enabled);
        Assert.Equal(SpeedMultiplierOption.x5, config.SpeedMultiplier.Value);
        Assert.Equal(5f, s.SpeedMultiplier);
        Assert.False(s.InstantFinish);
        Assert.False(s.DebugLogging);
    }

    [Fact]
    public void Generated_file_lists_the_drop_down_options()
    {
        Bind();
        string text = File.ReadAllText(_path);

        Assert.Contains("[Frame Finishing]", text);
        Assert.Contains("Enabled = true", text);
        Assert.Contains("SpeedMultiplier = x5", text);
        Assert.Contains("# Acceptable values: x2, x3, x4, x5, x10", text);
        Assert.Contains("InstantFinish = false", text);
        Assert.Contains("DebugLogging = false", text);
        Assert.Contains("restarting the game", text);
    }

    [Fact]
    public void Drop_down_offers_exactly_2x_3x_4x_5x_10x()
    {
        var labels = typeof(SpeedMultiplierOption).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => f.GetCustomAttribute<DisplayAttribute>()!.GetName())
            .ToArray();

        Assert.Equal(new[] { "2x", "3x", "4x", "5x", "10x" }, labels);
        Assert.Equal(new[] { 2, 3, 4, 5, 10 }, Enum.GetValues<SpeedMultiplierOption>().Select(v => (int)v).ToArray());
    }

    [Theory]
    [InlineData("x2", 2f)]
    [InlineData("x10", 10f)]
    [InlineData("5", 5f)]   // config written by older versions (plain number)
    [InlineData("10", 10f)]
    [InlineData("3", 3f)]
    public void Option_names_and_old_numeric_values_are_read(string written, float expected)
    {
        WriteFile(written);

        FinishingConfig config = Bind();

        Assert.Equal(expected, config.CreateSettings().SpeedMultiplier);
        Assert.Null(config.LastRejectedSpeedMultiplier);
    }

    [Theory]
    [InlineData("7", 7)]
    [InlineData("100", 100)]
    [InlineData("0", 0)]
    public void Numbers_that_are_not_options_fall_back_to_5x_and_are_written_back(string written, int rejected)
    {
        WriteFile(written);

        FinishingConfig config = Bind();

        Assert.Equal(SpeedMultiplierOption.x5, config.SpeedMultiplier.Value);
        Assert.Equal(5f, config.CreateSettings().SpeedMultiplier);
        Assert.Equal(rejected, config.LastRejectedSpeedMultiplier);
        Assert.Contains("SpeedMultiplier = x5", File.ReadAllText(_path));
    }

    [Theory]
    [InlineData("2.5")]
    [InlineData("fast")]
    public void Unparseable_values_use_the_default(string written)
    {
        WriteFile(written);

        Assert.Equal(5f, Bind().CreateSettings().SpeedMultiplier);
    }

    [Fact]
    public void Entries_carry_LaunchPad_ui_hints_and_multiplier_is_an_enum_drop_down()
    {
        FinishingConfig config = Bind();

        Assert.True(config.SpeedMultiplier.SettingType.IsEnum); // LaunchPad draws enums as a combo box
        AssertHints(config.Enabled, 0, "Enabled");
        AssertHints(config.SpeedMultiplier, 1, "Speed Multiplier");
        AssertHints(config.InstantFinish, 2, "Instant Mode");
        AssertHints(config.DebugLogging, 3, "Debug Logging");
    }

    [Fact]
    public void Disabled_in_file_yields_vanilla_settings()
    {
        File.WriteAllText(_path, "[Frame Finishing]\nEnabled = false\nSpeedMultiplier = x10\nInstantFinish = true\nDebugLogging = false\n");

        FinishingSettings s = Bind().CreateSettings();
        Assert.False(s.Enabled);
        Assert.Equal(2.0f, FinishingTimeCalculator.Compute(2.0f, s));
    }

    private static void AssertHints(ConfigEntryBase entry, int order, string displayName)
    {
        object[] tags = entry.Description.Tags;
        Assert.Contains(new KeyValuePair<string, int>("Order", order), tags);
        Assert.Contains(new KeyValuePair<string, string>("DisplayName", displayName), tags);
        Assert.Contains(new KeyValuePair<string, bool>("RequireRestart", true), tags);
    }
}
#endif
