using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using FasterFrameFinishing.Core;

namespace FasterFrameFinishing.Configuration
{
    /// <summary>Binds the BepInEx config file and produces validated <see cref="FinishingSettings"/>.</summary>
    /// <remarks>
    /// Tags are StationeersLaunchPad config-UI hints (verified in SLP 1.2.1 ConfigEntryWrapper): plain
    /// KeyValuePair values, so no LaunchPad reference is needed and BepInEx ignores them.
    /// SpeedMultiplier is an enum, which LaunchPad draws as a drop-down (2x, 3x, 4x, 5x, 10x).
    /// </remarks>
    internal sealed class FinishingConfig
    {
        internal const string Section = "Frame Finishing";
        internal const SpeedMultiplierOption DefaultSpeed = SpeedMultiplierOption.x5;
        private const string RestartNote = " Changes take effect after restarting the game.";

        private FinishingConfig(ConfigFile file)
        {
            Enabled = file.Bind(Section, "Enabled", true, new ConfigDescription(
                "Master switch. Off = normal (vanilla) finishing time; the mod changes nothing." + RestartNote,
                null, Hints(0, "Enabled")));

            SpeedMultiplier = file.Bind(Section, "SpeedMultiplier", DefaultSpeed, new ConfigDescription(
                "How many times faster welding sheets onto iron and steel frames, and the plastic-sheet finishing " +
                "step on steel frames, are: 2x, 3x, 4x, 5x or 10x. At 5x (default), each welding step takes 0.2 s " +
                "instead of 1.0 s, and finishing takes 0.4 s instead of 2.0 s. Placing frames and taking them apart " +
                "are unchanged. For normal speed, turn Enabled off." + RestartNote,
                null, Hints(1, "Speed Multiplier")));

            InstantFinish = file.Bind(Section, "InstantFinish", false, new ConfigDescription(
                "Make welding and finishing frames almost instant, whatever the Speed Multiplier. " +
                "You still use the welder, sheets, screwdriver and plastic as normal." + RestartNote,
                null, Hints(2, "Instant Mode")));

            DebugLogging = file.Bind(Section, "DebugLogging", false, new ConfigDescription(
                "Log every frame prefab's build states and matching decisions to the BepInEx log." + RestartNote,
                null, Hints(3, "Debug Logging")));

            NormalizeSpeedMultiplier();
            SpeedMultiplier.SettingChanged += (s, e) => NormalizeSpeedMultiplier();
        }

        public ConfigEntry<bool> Enabled { get; }

        public ConfigEntry<SpeedMultiplierOption> SpeedMultiplier { get; }

        public ConfigEntry<bool> InstantFinish { get; }

        public ConfigEntry<bool> DebugLogging { get; }

        /// <summary>Set when the file held a number that is not one of the options; holds that number.</summary>
        public int? LastRejectedSpeedMultiplier { get; private set; }

        public static FinishingConfig Bind(ConfigFile file) => new FinishingConfig(file);

        public FinishingSettings CreateSettings() =>
            new FinishingSettings(Enabled.Value, ToMultiplier(SpeedMultiplier.Value), InstantFinish.Value, DebugLogging.Value);

        /// <summary>The numeric multiplier for an option; undefined values map to the default.</summary>
        public static float ToMultiplier(SpeedMultiplierOption option) =>
            Enum.IsDefined(typeof(SpeedMultiplierOption), option) ? (int)option : (int)DefaultSpeed;

        /// <summary>Raised when any of this mod's settings changes while the game is running.</summary>
        public void OnAnySettingChanged(Action<ConfigEntryBase> handler)
        {
            Enabled.SettingChanged += (s, e) => handler(Enabled);
            SpeedMultiplier.SettingChanged += (s, e) => handler(SpeedMultiplier);
            InstantFinish.SettingChanged += (s, e) => handler(InstantFinish);
            DebugLogging.SettingChanged += (s, e) => handler(DebugLogging);
        }

        /// <summary>
        /// A hand-edited number that parses but is not an option (e.g. "SpeedMultiplier = 7") is replaced
        /// by the default and written back, so the drop-down and the .cfg always show a real option.
        /// </summary>
        private void NormalizeSpeedMultiplier()
        {
            SpeedMultiplierOption current = SpeedMultiplier.Value;
            if (!Enum.IsDefined(typeof(SpeedMultiplierOption), current))
            {
                LastRejectedSpeedMultiplier = (int)current;
                SpeedMultiplier.Value = DefaultSpeed;
            }
        }

        private static object[] Hints(int order, string displayName) => new object[]
        {
            new KeyValuePair<string, int>("Order", order),
            new KeyValuePair<string, string>("DisplayName", displayName),
            new KeyValuePair<string, bool>("RequireRestart", true),
        };

        internal static string Describe(SpeedMultiplierOption option) =>
            ((int)option).ToString(CultureInfo.InvariantCulture) + "x";
    }
}
