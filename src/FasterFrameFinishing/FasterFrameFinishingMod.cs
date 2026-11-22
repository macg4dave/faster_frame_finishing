using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using FasterFrameFinishing.Configuration;
using FasterFrameFinishing.Core;
using FasterFrameFinishing.Game;
using UnityEngine;

namespace FasterFrameFinishing
{
    /// <summary>
    /// StationeersLaunchPad "Default Entrypoint". LaunchPad finds any MonoBehaviour in the mod's
    /// assemblies with a method named <c>OnLoaded</c> whose parameters are drawn from
    /// { ConfigFile, List&lt;GameObject&gt;, List&lt;Assembly&gt;, ModData }, adds it as a component
    /// and calls OnLoaded. The injected ConfigFile is BepInEx/config/&lt;About.xml ModID&gt;.cfg and is
    /// editable from LaunchPad's configuration window.
    ///
    /// Deliberately not a BaseUnityPlugin: LaunchPad registers BepInEx and Default entrypoints
    /// independently, so being both would initialise the mod twice. It also does not reference
    /// StationeersLaunchPad.dll, whose classes are not a stable API.
    ///
    /// This class references no Stationeers types; all game access goes through FinishingBootstrap.
    /// </summary>
    public sealed class FasterFrameFinishingMod : MonoBehaviour
    {
        private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.Name);

        /// <summary>Called by StationeersLaunchPad before the game loads its prefabs.</summary>
        public void OnLoaded(ConfigFile config)
        {
            try
            {
                Initialize(config);
            }
            catch (Exception ex)
            {
                // Typically MissingMemberException / TypeLoadException after a game update.
                Log.LogError("Initialisation failed; the mod is inactive and the game is unmodified. " + ex);
            }
        }

        private static void Initialize(ConfigFile file)
        {
            if (file == null)
            {
                Log.LogError("StationeersLaunchPad did not provide a config file; the mod is inactive.");
                return;
            }

            FinishingConfig config = FinishingConfig.Bind(file);
            FinishingSettings settings = config.CreateSettings();
            config.OnAnySettingChanged(entry =>
                Log.LogWarning($"'{entry.Definition.Key}' changed to '{entry.BoxedValue}'. Restart the game for it to take effect."));

            if (!settings.SpeedMultiplier.Equals(config.SpeedMultiplier.Value)) // also catches NaN
            {
                Log.LogWarning($"SpeedMultiplier '{config.SpeedMultiplier.Value}' is invalid; using {settings.SpeedMultiplier}.");
            }

            Log.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded via StationeersLaunchPad. {settings}");

            if (!settings.Enabled)
            {
                Log.LogInfo("Disabled in config: frame finishing keeps vanilla timing and no game data is modified.");
                return;
            }

            FinishingBootstrap.Start(settings, Log);
        }
    }
}
