using System.Runtime.CompilerServices;
using BepInEx.Logging;
using FasterFrameFinishing.Core;

namespace FasterFrameFinishing.Game
{
    /// <summary>
    /// Owns the patcher and its event subscription. The plugin class only references this type
    /// through a guarded call, so a missing game type can never break loading of the plugin class.
    /// The patcher is kept statically so it does not depend on the plugin GameObject surviving.
    /// </summary>
    internal static class FinishingBootstrap
    {
        private static FramePrefabPatcher _patcher;

        public static bool IsStarted => _patcher != null;

        /// <summary>Hooks prefab loading. Returns false if already started. May throw if the game API changed.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Start(FinishingSettings settings, ManualLogSource log)
        {
            if (_patcher != null)
            {
                log.LogWarning("Plugin initialised more than once; keeping the existing patcher.");
                return false;
            }

            var patcher = new FramePrefabPatcher(settings, log);
            GameHooks.SubscribePrefabsLoaded(patcher.ApplySafely);
            _patcher = patcher;

            if (GameHooks.StructurePrefabsAvailable())
            {
                log.LogInfo("Structure prefabs already loaded; applying now.");
                patcher.ApplySafely();
            }

            return true;
        }

        /// <summary>Unhooks and restores vanilla values. Never throws. Not called automatically.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Stop(ManualLogSource log)
        {
            FramePrefabPatcher patcher = _patcher;
            if (patcher == null)
            {
                return;
            }

            try
            {
                GameHooks.UnsubscribePrefabsLoaded(patcher.ApplySafely);
            }
            catch (System.Exception ex)
            {
                log.LogError("Failed to unhook prefab loading: " + ex);
            }

            patcher.RestoreSafely();
            _patcher = null;
        }
    }
}
