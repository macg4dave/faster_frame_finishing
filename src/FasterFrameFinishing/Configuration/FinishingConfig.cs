using System;
using BepInEx.Configuration;
using FasterFrameFinishing.Core;

namespace FasterFrameFinishing.Configuration
{
    /// <summary>Binds the BepInEx config file and produces validated <see cref="FinishingSettings"/>.</summary>
    internal sealed class FinishingConfig
    {
        internal const string Section = "Frame Finishing";
        private const string RestartNote = " Changes take effect after restarting the game.";

        private FinishingConfig(ConfigFile file)
        {
            Enabled = file.Bind(Section, "Enabled", true,
                "Master switch. false = vanilla finishing time (the mod changes nothing)." + RestartNote);

            SpeedMultiplier = file.Bind(Section, "SpeedMultiplier", FinishingSettings.DefaultSpeedMultiplier,
                new ConfigDescription(
                    "How much faster the plastic-sheet + screwdriver finishing stage of steel frames is. " +
                    "1.0 = vanilla, 5.0 = five times faster (vanilla 2.0 s becomes 0.4 s before tool-speed modifiers). " +
                    "Values below 1.0 slow it down. Durations never go below " +
                    FinishingTimeCalculator.MinimumEntryTime.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                    " s." + RestartNote,
                    new AcceptableValueRange<float>(FinishingSettings.MinSpeedMultiplier, FinishingSettings.MaxSpeedMultiplier)));

            InstantFinish = file.Bind(Section, "InstantFinish", false,
                "If true, finishing completes almost instantly regardless of SpeedMultiplier. " +
                "You still use the screwdriver and plastic sheet as normal." + RestartNote);

            DebugLogging = file.Bind(Section, "DebugLogging", false,
                "Log every steel-frame prefab's build states and matching decisions to the BepInEx log." + RestartNote);
        }

        public ConfigEntry<bool> Enabled { get; }

        public ConfigEntry<float> SpeedMultiplier { get; }

        public ConfigEntry<bool> InstantFinish { get; }

        public ConfigEntry<bool> DebugLogging { get; }

        public static FinishingConfig Bind(ConfigFile file) => new FinishingConfig(file);

        public FinishingSettings CreateSettings() =>
            new FinishingSettings(Enabled.Value, SpeedMultiplier.Value, InstantFinish.Value, DebugLogging.Value);

        /// <summary>Raised when any of this mod's settings changes while the game is running.</summary>
        public void OnAnySettingChanged(Action<ConfigEntryBase> handler)
        {
            Enabled.SettingChanged += (s, e) => handler(Enabled);
            SpeedMultiplier.SettingChanged += (s, e) => handler(SpeedMultiplier);
            InstantFinish.SettingChanged += (s, e) => handler(InstantFinish);
            DebugLogging.SettingChanged += (s, e) => handler(DebugLogging);
        }
    }
}
