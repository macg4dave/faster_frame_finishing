#if HAS_BEPINEX
using System;
using System.IO;
using BepInEx.Configuration;
using FasterFrameFinishing.Configuration;
using FasterFrameFinishing.Core;
using Xunit;

namespace FasterFrameFinishing.Tests;

/// <summary>
/// Binds the real FinishingConfig against BepInEx's real ConfigFile (from the local install) and
/// checks defaults, the generated .cfg text, and range validation of hand-edited values.
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

    [Fact]
    public void Defaults_match_specification()
    {
        FinishingSettings s = Bind().CreateSettings();

        Assert.True(s.Enabled);
        Assert.Equal(5f, s.SpeedMultiplier);
        Assert.False(s.InstantFinish);
        Assert.False(s.DebugLogging);
    }

    [Fact]
    public void Generated_file_contains_expected_section_and_keys()
    {
        Bind();
        string text = File.ReadAllText(_path);

        Assert.Contains("[Frame Finishing]", text);
        Assert.Contains("Enabled = true", text);
        Assert.Contains("SpeedMultiplier = 5", text);
        Assert.Contains("InstantFinish = false", text);
        Assert.Contains("DebugLogging = false", text);
        Assert.Contains("Acceptable value range: From 0.1 to 100", text);
        Assert.Contains("restarting the game", text);
    }

    [Theory]
    [InlineData("1000", 100f)]
    [InlineData("0.001", 0.1f)]
    [InlineData("1", 1f)]
    [InlineData("2.5", 2.5f)]
    public void Hand_edited_multiplier_is_clamped(string written, float expected)
    {
        File.WriteAllText(_path, $"[Frame Finishing]\nEnabled = true\nSpeedMultiplier = {written}\nInstantFinish = false\nDebugLogging = false\n");

        Assert.Equal(expected, Bind().CreateSettings().SpeedMultiplier, 1e-5f);
    }

    [Fact]
    public void Disabled_in_file_yields_vanilla_settings()
    {
        File.WriteAllText(_path, "[Frame Finishing]\nEnabled = false\nSpeedMultiplier = 5\nInstantFinish = true\nDebugLogging = false\n");

        FinishingSettings s = Bind().CreateSettings();
        Assert.False(s.Enabled);
        Assert.Equal(2.0f, FinishingTimeCalculator.Compute(2.0f, s));
    }
}
#endif
